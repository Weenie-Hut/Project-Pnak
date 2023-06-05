using System.Linq;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkModScripts : ScriptableObject
	{
#if UNITY_EDITOR
		private static LiteNetworkModScripts _instance;
		public static LiteNetworkModScripts RawInstance => _instance;
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

		public static void StaticRefreshRefs()
		{
			Instance.RefreshRefs();
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

		public static T[] Add<T>(T[] array, T obj) where T : Object
		{
			T[] result;
			if (array == null)
				result = new T[1] { obj };
			else if (array.Contains(obj)) result = array;
			else
			{
				// try to find a null slot
				int index = System.Array.IndexOf(array, null);
				if (index == -1)
				{
					result = array.Concat(new T[1] { obj }).ToArray();
				}
				else
				{
					array[index] = obj;
					result = array;
				}
			}

			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;
			return result;
		}

		public static void AddLitePrefab(StateBehaviourController prefab)
		{
			Instance.LiteNetworkPrefabs = Add(Instance.LiteNetworkPrefabs, prefab);
		}
		
		public static void AddMod(LiteNetworkMod mod)
		{
			Instance.Mods = Add(Instance.Mods, mod);
		}

		public static void AddStateModifier(StateModifierSO stateModifier)
		{
			Instance.StateModifiers = Add(Instance.StateModifiers, stateModifier);
		}

		public static int ValidateIndex<T>(T[] array, T obj) where T : Object
		{
			if (array == null)
			{
				UnityEditor.EditorApplication.delayCall += StaticRefreshRefs;
				return -1;
			}
			return System.Array.IndexOf(array, obj);
		}

		public static int ValidateLitePrefabIndex(StateBehaviourController prefab)
			=> ValidateIndex(RawInstance?.LiteNetworkPrefabs, prefab);

		public static int ValidateModIndex(LiteNetworkMod mod)
			=> ValidateIndex(RawInstance?.Mods, mod);

		public static int ValidateStateModifierIndex(StateModifierSO stateModifier)
			=> ValidateIndex(RawInstance?.StateModifiers, stateModifier);

		public static T[] Remove<T>(T[] array, T obj) where T : Object
		{
			int index = System.Array.IndexOf(array, obj);
			if (index == -1) return array;

			array[index] = null;
			UnityEditor.EditorApplication.delayCall += Instance.RefreshRefs;

			return array;
		}

		public static void RemoveLitePrefab(StateBehaviourController prefab)
		{
			Instance.LiteNetworkPrefabs = Remove(Instance.LiteNetworkPrefabs, prefab);
		}

		public static void RemoveMod(LiteNetworkMod mod)
		{
			Instance.Mods = Remove(Instance.Mods, mod);
		}

		public static void RemoveStateModifier(StateModifierSO stateModifier)
		{
			Instance.StateModifiers = Remove(Instance.StateModifiers, stateModifier);
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