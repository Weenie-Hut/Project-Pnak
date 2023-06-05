using System.Linq;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkModScripts : ScriptableObject
	{
#if UNITY_EDITOR
		private static LiteNetworkModScripts _instance;
		public static LiteNetworkModScripts Instance
		{
			get
			{
				if (_instance == null)
				{
					string guid = UnityEditor.AssetDatabase.FindAssets("t:LiteNetworkModScripts")[0];
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
					_instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LiteNetworkModScripts>(path);
				}
				return _instance;
			}
		}

		public static string[] FieldNames;
		public static GUIContent[] ModOptions;

		private void OnValidate()
		{
			_instance = this;

			UnityEditor.EditorApplication.delayCall += RefreshRefs;
		}

		public void RefreshRefs()
		{
			int modsLength = Mods == null ? 0 : Mods.Length;

			FieldNames = new string[modsLength + 1];
			ModOptions = new GUIContent[modsLength + 1];

			FieldNames[0] = null;
			ModOptions[0] = new GUIContent("< Select Mod >");

			System.Reflection.FieldInfo[] fields = typeof(LiteNetworkedData).GetFields();
			for (int i = 0; i < modsLength; i++)
			{
				if (Mods[i] == null)
				{
					FieldNames[i + 1] = null;
					ModOptions[i + 1] = new GUIContent("< Missing Mod >");
					continue;
				}

				FieldNames[i + 1] = Mods[i].DataType.Name + "Field";
				ModOptions[i + 1] = new GUIContent(Mods[i].name);

				Mods[i].EditorSetScriptIndex(i);
				UnityEditor.EditorUtility.SetDirty(Mods[i]);
			}

			int stateModifiersLength = StateModifiers == null ? 0 : StateModifiers.Length;
			for (int i = 0; i < stateModifiersLength; i++)
			{
				if (StateModifiers[i] == null)
					continue;

				StateModifiers[i].EditorSetTypeIndex(i);
				UnityEditor.EditorUtility.SetDirty(StateModifiers[i]);
			}

			StateRunnerMod stateRunnerMod = System.Array.Find(Mods, (LiteNetworkMod mod) => mod is StateRunnerMod) as StateRunnerMod;
			if (stateRunnerMod != null)
			{
				int liteNetworkPrefabsLength = LiteNetworkPrefabs == null ? 0 : LiteNetworkPrefabs.Length;
				for (int i = 0; i < liteNetworkPrefabsLength; i++)
				{
					if (LiteNetworkPrefabs[i] == null) continue;

					LiteNetworkPrefabs[i].SetHiddenSerializedFields(i, stateRunnerMod);
					UnityEditor.EditorUtility.SetDirty(LiteNetworkPrefabs[i]);
				}
			}

			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();
		}

		public static void AddLitePrefab(StateBehaviourController prefab)
		{
			if (Instance.LiteNetworkPrefabs == null)
				Instance.LiteNetworkPrefabs = new StateBehaviourController[1] { prefab };
			else{
				if (Instance.LiteNetworkPrefabs.Contains(prefab)) return;

				// try to find a null slot
				int index = System.Array.IndexOf(Instance.LiteNetworkPrefabs, null);
				if (index == -1)
				{
					Instance.LiteNetworkPrefabs = Instance.LiteNetworkPrefabs.Concat(new StateBehaviourController[1] { prefab }).ToArray();
				}
				else Instance.LiteNetworkPrefabs[index] = prefab;
			}

			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;
		}
		
		public static void AddMod(LiteNetworkMod mod)
		{
			if (Instance.Mods == null)
				Instance.Mods = new LiteNetworkMod[1] { mod };
			else{
				if (Instance.Mods.Contains(mod)) return;

				// try to find a null slot
				int index = System.Array.IndexOf(Instance.Mods, null);
				if (index == -1)
				{
					Instance.Mods = Instance.Mods.Concat(new LiteNetworkMod[1] { mod }).ToArray();
				}
				else Instance.Mods[index] = mod;
			}

			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;
		}

		public static void AddStateModifier(StateModifierSO stateModifier)
		{
			if (Instance.StateModifiers == null)
				Instance.StateModifiers = new StateModifierSO[1] { stateModifier };
			else{
				if (Instance.StateModifiers.Contains(stateModifier)) return;

				// try to find a null slot
				int index = System.Array.IndexOf(Instance.StateModifiers, null);
				if (index == -1)
				{
					Instance.StateModifiers = Instance.StateModifiers.Concat(new StateModifierSO[1] { stateModifier }).ToArray();
				}
				else Instance.StateModifiers[index] = stateModifier;
			}

			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;
		}

		public static int ValidateLitePrefabIndex(StateBehaviourController prefab)
		{
			if (Instance.LiteNetworkPrefabs == null) return -1;
			return System.Array.IndexOf(Instance.LiteNetworkPrefabs, prefab);
		}

		public static int ValidateModIndex(LiteNetworkMod mod)
		{
			if (Instance.Mods == null) return -1;
			return System.Array.IndexOf(Instance.Mods, mod);
		}

		public static int ValidateStateModifierIndex(StateModifierSO stateModifier)
		{
			if (Instance.StateModifiers == null) return -1;
			return System.Array.IndexOf(Instance.StateModifiers, stateModifier);
		}

		public static int RemoveLitePrefab(StateBehaviourController prefab)
		{
			int index = System.Array.IndexOf(Instance.LiteNetworkPrefabs, prefab);
			if (index == -1) return -1;

			Instance.LiteNetworkPrefabs[index] = null;
			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;

			return index;
		}

		public static int RemoveMod(LiteNetworkMod mod)
		{
			int index = System.Array.IndexOf(Instance.Mods, mod);
			if (index == -1) return -1;

			Instance.Mods[index] = null;
			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;

			return index;
		}

		public static int RemoveStateModifier(StateModifierSO stateModifier)
		{
			int index = System.Array.IndexOf(Instance.StateModifiers, stateModifier);
			if (index == -1) return -1;

			Instance.StateModifiers[index] = null;
			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;

			return index;
		}
#endif
		[ReadOnly, NonReorderable]
		public StateBehaviourController[] LiteNetworkPrefabs;

		[ReadOnly, NonReorderable]
		public LiteNetworkMod[] Mods;

		[ReadOnly, NonReorderable]
		public StateModifierSO[] StateModifiers;
	}
}