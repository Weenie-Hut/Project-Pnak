using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pnak
{
	[System.Serializable]
	public struct SceneAsset
	{
		[SerializeField] private int _buildIndex;
		public int BuildIndex => _buildIndex;

#if UNITY_EDITOR
		[SerializeField] private UnityEditor.SceneAsset _sceneAsset;
#endif
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(SceneAsset))]
	public class SceneAssetDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
		{
			var buildIndex = property.FindPropertyRelative("_buildIndex");
			var sceneAsset = property.FindPropertyRelative("_sceneAsset");

			UnityEditor.EditorGUI.BeginChangeCheck();
			UnityEditor.EditorGUI.PropertyField(position, sceneAsset, label);
			if (UnityEditor.EditorGUI.EndChangeCheck())
			{
				buildIndex.intValue = sceneAsset.objectReferenceValue == null
					? -1
					: UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(UnityEditor.AssetDatabase.GetAssetPath(sceneAsset.objectReferenceValue)).buildIndex;
				UnityEngine.Debug.Log(buildIndex);
			}
		}
	}
#endif
}