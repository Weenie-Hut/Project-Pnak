using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

namespace PnakEditor
{
	internal class ObjectPickerEntry
	{
		public string name;
		public UnityEngine.Object obj;

		public ObjectPickerEntry(string name, UnityEngine.Object obj)
		{
			this.name = name;
			this.obj = obj;
		}

		public string Item1 => name;
		public UnityEngine.Object Item2 => obj;

		[System.Flags]
		public enum IncludeDropdowns
		{
			Scene = 1 << 0,
			Assets = 1 << 1,
			All = Scene | Assets
		}

		public static Dictionary<string, ObjectPickerEntry[]> CreateObjectPickerDictionary(SerializedProperty property, Predicate<GameObject> filter, IncludeDropdowns include = IncludeDropdowns.All)
		{
			Dictionary<string, ObjectPickerEntry[]> objectOptions = new Dictionary<string, ObjectPickerEntry[]>();

			if (include.HasFlag(IncludeDropdowns.Scene))
			{
				objectOptions.Add("Scene", GetSceneObjects(property, filter));
			}

			if (include.HasFlag(IncludeDropdowns.Assets))
			{
				objectOptions.Add("Assets", GetAssetObject(filter));
			}

			return objectOptions;
		}

		public static ObjectPickerEntry[] GetAssetObject(Predicate<GameObject> filter)
		{
			return AssetDatabase.FindAssets("t:prefab")
				.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
				.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
				.Where(go => filter(go))
				.Select(go => new ObjectPickerEntry(go.name, go))
				.ToArray();
		}


		public static ObjectPickerEntry[] GetSceneObjects(SerializedProperty property, Predicate<GameObject> filter)
		{
			Scene? scene = GetRelevantScene(property);
			if (scene == null || !scene.Value.IsValid())
				return new ObjectPickerEntry[0];

			return scene.Value.GetRootGameObjects()
				.SelectMany(rootGameObject => GetAllChildObjects(rootGameObject))
				.Where(go => filter(go))
				.Select(go => new ObjectPickerEntry(go.name, go))
				.ToArray();
		}

		public static IEnumerable<GameObject> GetAllChildObjects(GameObject parent)
		{
			yield return parent;
			foreach (Transform child in parent.transform)
			{
				yield return child.gameObject;
				foreach (GameObject grandChild in GetAllChildObjects(child.gameObject))
					yield return grandChild;
			}
		}

		private static Scene? GetRelevantScene(SerializedProperty property)
		{
			UnityEngine.Object target = property.serializedObject.targetObject;

			if (target is ScriptableObject)
				return null;
			if (target is Component component)
				return component.gameObject.scene;
			if (target is GameObject gameObject)
				return gameObject.scene;

			return null;
		}
	}
}