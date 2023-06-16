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
		// TODO: This is copied to all instances created, but only needed on the prefab. Maybe use serialized object?
		public SerializedLiteNetworkedData[] SerializedMods;

		private LiteNetworkedData[] data;
		public LiteNetworkedData[] Data
		{
			get {
				if (data == null)
				{
					data = new LiteNetworkedData[SerializedMods.Length];
					for (int i = 0; i < SerializedMods.Length; i++)
						data[i] = SerializedMods[i].ToLiteNetworkedData();
				}
				return data;
			}
		}

		[Header("Read Only")]
		[SerializeField, NonReorderable, ReadOnly] private StateBehaviour[] stateBehaviours;

		public void QueueForDestroy()
		{
			if (!LiteNetworkManager.QueueDeleteLiteObject(NetworkContext)) return;
			foreach (StateBehaviour stateBehaviour in stateBehaviours)
				stateBehaviour.QueuedForDestroy();
		}

		public void QueueSpawnHere(StateBehaviourController prefab)
		{
			LiteNetworkManager.QueueNewNetworkObject(prefab, HasTransform ? TransformCache.Value : new TransformData {
				Position = transform.position,
				RotationAngle = transform.localEulerAngles.z,
				Scale = transform.localScale
			});
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
			=> GetStateBehaviour<T>(0) as T;

		public T GetStateBehaviour<T>(int atOrder) where T : StateBehaviour
		{
			foreach (StateBehaviour stateBehaviour in stateBehaviours)
			{
				if (stateBehaviour is T)
				{
					if (atOrder == 0)
						return (T)stateBehaviour;
					atOrder--;
				}
			}
			return null;
		}

		public int FindModifierAddress(int scriptIndex) => FindModifierAddress(scriptIndex, out _);
		public int FindModifierAddress(int scriptIndex, out LiteNetworkedData data)
		{
			foreach(int addr in NetworkContext.Modifiers)
			{
				data = LiteNetworkManager.GetModifierData(addr);
				if (data.ScriptType == scriptIndex)
					return addr;
			}

			data = default;
			return -1;
		}

		public int GetBehaviourTypeIndex<T>(T behaviour) where T : StateBehaviour
		{
			int index = -1;
			foreach (StateBehaviour stateBehaviour in stateBehaviours)
			{
				if (stateBehaviour is T)
				{
					index++;
					if (stateBehaviour == behaviour)
						return index;
				}
			}
			return -1;
		}

		private void Awake()
		{
			TransformCache = new CacheCallbacks<TransformData>(GetTransformData, SetTransformData);
			TransformCache.Enabled = false;
		}

		public Cache<TransformData> TransformCache { get; private set; }

		private TransformData GetTransformData()
		{
			if (TransformModIndex == -1)
			{
				UnityEngine.Debug.LogWarning("Trying to get transform data on object that does not have a transform mod: " + gameObject.name);
				return default;
			}

			return TransformScript.GetTransformData(TransformModIndex);
		}

		private void SetTransformData(ref TransformData value)
		{
			var data = LiteNetworkManager.GetModifierData(TransformModIndex);
			TransformScript.UpdateTransform(ref data, value);
			LiteNetworkManager.SetModifierData(TransformModIndex, data);
		}

		public PlayerRef InputAuthority => NetworkContext.InputAuthority;
		public NetworkInputData? Input { get; private set; }

		public int FindNetworkMod<T>(out int scriptType) where T : LiteNetworkMod
			=> FindNetworkMod<T>(0, out scriptType);

		public int FindNetworkMod<T>(int atOrder, out int scriptType) where T : LiteNetworkMod
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
					if (atOrder == 0)
					{
						scriptType = data.ScriptType;
						return modifierAddress;
					}
					atOrder--;
				}
			}
			scriptType = -1;
			return -1;
		}

		// private delegate void UpdateNetworkData(ref LiteNetworkedData runnerData);
		// private event UpdateNetworkData updateNetworkData = null;
		// private Queue<StateModifier> RemoveStateModQueue = new Queue<StateModifier>();
		// private Queue<StateModifier> AddStateModQueue = new Queue<StateModifier>();
		public void InputFixedUpdateNetwork()
		{
			Input = SessionManager.Instance.NetworkRunner.GetInputForPlayer<NetworkInputData>(InputAuthority);
			TransformCache.Enabled = true;

			foreach (StateBehaviour state in stateBehaviours)
			{
				if (state.enabled) state.InputFixedUpdateNetwork();
			}

			TransformCache.Apply();
			TransformCache.Enabled = false;
		}

		public void FixedUpdateNetwork()
		{
			TransformCache.Enabled = true;

			foreach (StateBehaviour state in stateBehaviours)
			{
				if (state.enabled) state.FixedUpdateNetwork();
			}

			TransformCache.Apply();
			TransformCache.Enabled = false;
		}

		internal void Render()
		{
			foreach (StateBehaviour state in stateBehaviours)
			{
				state.Render();
			}
		}

		public void Initialize(LiteNetworkObject networkContext)
		{
			UnityEngine.Debug.Assert(NetworkContext == null);
			UnityEngine.Debug.Assert(networkContext != null);
			UnityEngine.Debug.Assert(networkContext.PrefabIndex == PrefabIndex);
			UnityEngine.Debug.Assert(networkContext.Target == this);

			NetworkContext = networkContext;

			if (SessionManager.IsServer)
			{
				foreach (StateBehaviour state in stateBehaviours)
				{
					state.FixedInitialize();
				}
			}
		}

		// private int predictedDestroyTick = -1;
		// public void SetPredictedDestroyTick(int tick)
		// {
		// 	predictedDestroyTick = tick;
		// 	updateNetworkData += _SetPredictedDestroyTick;
		// }

		// private void _SetPredictedDestroyTick(ref LiteNetworkedData data)
		// {
		// 	data.StateRunner.predictedDestroyTick = predictedDestroyTick;
		// 	predictedDestroyTick = -1;
		// }

#if UNITY_EDITOR
		[Button(nameof(AddToScripts), "Add", "Add script to the network config. Adding allows for multiplayer clients to sent a single number to identify the type of script/modifier that should be applied.", nameof(prefabIndex), -1, false)]
		[Button(nameof(RemoveFromScripts), "Rem", "Remove script from network config, freeing up unused asset. Adding allows for multiplayer clients to sent a single number to identify the type of script/modifier that should be applied.", nameof(prefabIndex), -1)]
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

		public virtual LiteNetworkedData[] GetDefaultMods(ref TransformData transform)
		{
			// UnityEngine.Debug.Log("GetDefaultMods() called on " + gameObject.name + " with " + stateBehaviours.Length + " state behaviours and " + Data.Length + " mods.");

			LiteNetworkedData[] mods = new LiteNetworkedData[Data.Length];
			System.Array.Copy(Data, mods, Data.Length);

			foreach (StateBehaviour state in stateBehaviours)
			{
				state.OnDataCreated(mods, ref transform);
			}

			return mods;
		}

#if UNITY_EDITOR
		internal void SetHiddenSerializedFields(int prefabIndex)
		{
			this.prefabIndex = prefabIndex;
			stateBehaviours = GetComponents<StateBehaviour>();
		}

		private void OnValidate()
		{
			stateBehaviours = GetComponents<StateBehaviour>();
			this.prefabIndex = LiteNetworkModScripts.ValidateLitePrefabIndex(this);
		}
#endif
	}
}