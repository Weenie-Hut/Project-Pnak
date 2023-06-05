using UnityEngine;
using System;
using System.Collections;
using Fusion;
using System.Collections.Generic;

namespace Pnak
{
	[DisallowMultipleComponent]
	public class StateBehaviourController : MonoBehaviour
	{
		public SerializedLiteNetworkedData[] DefaultMods;

		private LiteNetworkedData[] data;
		public LiteNetworkedData[] Data
		{
			get {
				if (data == null)
				{
					bool hasStateBehaviour = stateBehaviours.Length != 0;
					data = new LiteNetworkedData[DefaultMods.Length + (hasStateBehaviour ? 1 : 0)];
					for (int i = 0; i < DefaultMods.Length; i++)
						data[i] = DefaultMods[i].ToLiteNetworkedData();

					if (hasStateBehaviour)
					{
						StateRunnerMod.SetDefaults(ref data[DefaultMods.Length]);
					}
				}
				return data;
			}
		}

		[SerializeField, ReadOnly] private StateBehaviour[] stateBehaviours;

		private void Awake()
		{
			StateModifiers = new List<StateModifier>();
		}

		public bool QueuedForDestroy { get; private set; }
		public void QueueForDestroy()
		{
			QueuedForDestroy = true;
			LiteNetworkManager.QueueDeleteLiteObject(TargetNetworkIndex);
		}

		public LiteNetworkObject NetworkContext { get; private set; }
		public int TargetNetworkIndex => NetworkContext.Index;

		private int transformModIndex = int.MinValue;
		private TransformMod transformScript = null;
		public int TransformModIndex
		{
			get {
				if (transformModIndex == int.MinValue)
					SetTransformMod();
				return transformModIndex;
			}
		}
		public TransformMod TransformScript
		{
			get {
				if (transformScript == null)
					SetTransformMod();
				return transformScript;
			}
		}
		public bool HasTransform => TransformModIndex != -1;

		private void SetTransformMod()
		{
			transformModIndex = FindNetworkMod<TransformMod>(out int scriptType);
			if (transformModIndex == -1) return;
			transformScript = LiteNetworkManager.ModScripts[scriptType] as TransformMod;
		}

		public T GetStateBehaviour<T>() where T : StateBehaviour
		{
			foreach (StateBehaviour stateBehaviour in stateBehaviours)
			{
				if (stateBehaviour is T)
					return (T)stateBehaviour;
			}
			return null;
		}

		public List<StateModifier> StateModifiers { get; private set; }
		public void AddStateModifier(StateModifier modifier)
		{
			if (modifier == null) return;

			if (Stage == 1)
			{
				AddStateModQueue.Enqueue(modifier);
				return;
			}

			foreach (StateModifier existing in StateModifiers)
			{
				if (existing.TryStackWith(modifier))
					return;
			}

			StateModifiers.Add(modifier);
			modifier.Added(this);
		}

		public void RemoveStateModifier(StateModifier modifier)
		{
			if (modifier == null) return;

			if (Stage == 1)
			{
				RemoveStateModQueue.Enqueue(modifier);
				return;
			}

			StateModifiers.Remove(modifier);
			modifier.Removed();
		}

		public void CopyStateModifiersTo(LiteNetworkObject other)
		{
			foreach (StateModifier modifier in StateModifiers)
			{
				var copy = modifier.CopyFor(other.Target);

				if (copy != null)
					other.Target.AddStateModifier(copy);
			}
		}

		private TransformData? transformData = null;
		public TransformData TransformData
		{
			get {
				if (transformData != null) return transformData.Value;
				if (TransformModIndex == -1)
				{
					UnityEngine.Debug.LogWarning("Trying to get transform data on object that does not have a transform mod: " + gameObject.name);
					return default;
				}
				return TransformScript.GetTransformData(TransformModIndex);
			}
			set {
				if (TransformModIndex == -1)
				{
					UnityEngine.Debug.LogWarning("Trying to set transform data on object that does not have a transform mod: " + gameObject.name);
					return;
				}
				transformData = value;
				var data = LiteNetworkManager.GetModifierData(TransformModIndex);
				TransformScript.UpdateTransform(ref data, value);
				LiteNetworkManager.SetModifierData(TransformModIndex, data);
			}
		}

		public PlayerRef InputAuthority => LiteNetworkManager.GetInputAuth(TargetNetworkIndex);
		public NetworkInputData? Input { get; private set; }

		public int FindNetworkMod<T>(out int scriptType) where T : LiteNetworkMod
		{
			if (NetworkContext == null)
			{
				UnityEngine.Debug.LogWarning("Controller does not have any state behaviors and thus does not have a state runner to initialize network context. Use LiteNetworkManager.TryGetMod() instead.");
				scriptType = -1;
				return -1;
			}

			foreach (int modifierAddress in NetworkContext.Modifiers)
			{
				LiteNetworkedData data = LiteNetworkManager.GetModifierData(modifierAddress);
				if (LiteNetworkManager.ModScripts[data.ScriptType] is T)
				{
					scriptType = data.ScriptType;
					return modifierAddress;
				}
			}
			scriptType = -1;
			return -1;
		}

		private delegate void UpdateNetworkData(ref LiteNetworkedData runnerData);
		private event UpdateNetworkData updateNetworkData = null;
		public int Stage { get; private set; }
		private Queue<StateModifier> RemoveStateModQueue = new Queue<StateModifier>();
		private Queue<StateModifier> AddStateModQueue = new Queue<StateModifier>();
		public void FixedUpdateNetwork(ref LiteNetworkedData runnerData)
		{
			transformData = null;
			Input = SessionManager.Instance.NetworkRunner.GetInputForPlayer<NetworkInputData>(InputAuthority);

			Stage = 1;

			foreach (StateModifier modifier in StateModifiers)
			{
				modifier.FixedUpdateNetwork();
			}

			Stage = 2;

			while (RemoveStateModQueue.Count > 0)
			{
				StateModifier state = RemoveStateModQueue.Dequeue();
				RemoveStateModifier(state);
			}

			while (AddStateModQueue.Count > 0)
			{
				StateModifier state = AddStateModQueue.Dequeue();
				AddStateModifier(state);
			}

			Stage = 3;

			foreach (StateBehaviour state in stateBehaviours)
			{
				if (state.enabled) state.FixedUpdateNetwork();
			}

			if (updateNetworkData != null)
			{
				updateNetworkData(ref runnerData);
				updateNetworkData = null;
			}

			Stage = 4;
		}

		internal void Render()
		{
			Stage = 5;
			foreach (StateBehaviour state in stateBehaviours)
			{
				state.Render();
			}
			Stage = 0;
		}

		public void Initialize(LiteNetworkObject networkContext)
		{
			UnityEngine.Debug.Assert(NetworkContext == null);
			UnityEngine.Debug.Assert(networkContext != null);
			UnityEngine.Debug.Assert(networkContext.PrefabIndex == PrefabIndex);
			UnityEngine.Debug.Assert(networkContext.Target == this);

			NetworkContext = networkContext;

			foreach (StateBehaviour state in stateBehaviours)
			{
				state.Initialize();
			}
		}

		private int predictedDestroyTick = -1;
		public void SetPredictedDestroyTick(int tick)
		{
			predictedDestroyTick = tick;
			updateNetworkData += _SetPredictedDestroyTick;
		}

		private void _SetPredictedDestroyTick(ref LiteNetworkedData data)
		{
			data.StateRunner.predictedDestroyTick = predictedDestroyTick;
			predictedDestroyTick = -1;
		}
#if UNITY_EDITOR
		[Button(nameof(AddToScripts), "Add", nameof(prefabIndex) + "!=-1")]
		[Button(nameof(RemoveFromScripts), "Rem", nameof(prefabIndex) + "==-1")]
#endif
		[SerializeField, ReadOnly]
		private int prefabIndex = -1;

#if UNITY_EDITOR
		public void AddToScripts() => LiteNetworkModScripts.AddLitePrefab(this);
		public void RemoveFromScripts()
		{
			LiteNetworkModScripts.RemoveLitePrefab(this);
			prefabIndex = -1;
		}
#endif

		
		public int PrefabIndex => prefabIndex;
		[SerializeField, ReadOnly] private StateRunnerMod StateRunnerMod;

		public virtual int DefaultModsCount => Data.Length;
		public virtual LiteNetworkedData[] GetDefaultMods(ref TransformData transform)
		{
			LiteNetworkedData[] mods = new LiteNetworkedData[Data.Length];
			System.Array.Copy(Data, mods, Data.Length);

			foreach (StateBehaviour state in stateBehaviours)
			{
				state.OnDataCreated(mods, ref transform);
			}

			return mods;
		}

#if UNITY_EDITOR
		internal void SetHiddenSerializedFields(int prefabIndex, StateRunnerMod stateRunnerMod)
		{
			this.prefabIndex = prefabIndex;
			StateRunnerMod = stateRunnerMod;
			stateBehaviours = GetComponents<StateBehaviour>();
		}

		private void OnValidate()
		{
			stateBehaviours = GetComponents<StateBehaviour>();
			this.prefabIndex = LiteNetworkModScripts.ValidateLitePrefabIndex(this);
		}
#endif
	}
#if UNITY_EDITOR
// Create a custom property drawer for the StateBehaviourController class which will show all assets with a statebehaviourcontroller on the root object. Also, it will display a warning if the object does not have a prefab index set.
[UnityEditor.CustomPropertyDrawer(typeof(StateBehaviourController))]
public class StateBehaviourControllerDrawer : UnityEditor.PropertyDrawer
{
	public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
	{
		UnityEditor.EditorGUI.BeginProperty(position, label, property);
		
		// Get the object:
		StateBehaviourController controller = property.objectReferenceValue as StateBehaviourController;
		int prefabIndex = controller?.PrefabIndex ?? -2;

		if (prefabIndex != -2)
		{
			GUIContent content = new GUIContent("ID: " + prefabIndex.ToString());
			GUIStyle style = UnityEditor.EditorStyles.miniLabel;
			Vector2 size = style.CalcSize(content);

			Rect copy = position;
			copy.width -= size.x;
			UnityEditor.EditorGUI.PropertyField(copy, property, label, true);

			copy.x += copy.width;
			copy.width = size.x;
			copy.height = UnityEditor.EditorGUIUtility.singleLineHeight;
			UnityEditor.EditorGUI.LabelField(copy, "ID: " + prefabIndex.ToString(), style);
		}
		else {
			UnityEditor.EditorGUI.PropertyField(position, property, label, true);
		}

		if (prefabIndex == -1)
		{
			// Display a warning if the prefab index is not set. Get the prefered height of the warning box:
			float height = UnityEditor.EditorGUIUtility.singleLineHeight * 2;
			UnityEditor.EditorGUI.HelpBox(new Rect(position.x, position.y + UnityEditor.EditorGUIUtility.singleLineHeight, position.width, height), "Target has not been added to the Network Prefab pool!", UnityEditor.MessageType.Error);
		}

		UnityEditor.EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
	{
		float height = UnityEditor.EditorGUI.GetPropertyHeight(property, label);

		// Get the object:
		StateBehaviourController controller = property.objectReferenceValue as StateBehaviourController;
		int prefabIndex = controller?.PrefabIndex ?? -2;
		if (prefabIndex == -1)
			height += UnityEditor.EditorGUIUtility.singleLineHeight * 2;

		return height;
	}
}
#endif

}