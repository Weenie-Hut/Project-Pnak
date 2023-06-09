using UnityEditor;
using Fusion.Editor;
using Fusion;
using UnityEngine;

public abstract partial class SpawnPointManagerPrototype<T>
{
//   [BehaviourAction]
//   protected void DrawFoundSpawnPointCount() {
//     if (Application.isPlaying == false) {
//       GUILayout.BeginVertical(FusionGUIStyles.GroupBoxType.Info.GetStyle());
//       GUILayout.Space(4);
//       if (GUI.Button(EditorGUILayout.GetControlRect(), "Find Spawn Points")) {
//         _spawnPoints.Clear();
//         var found = UnityEngine.SceneManagement.SceneManager.GetActiveScene().FindObjectsOfTypeInOrder<T, Component>();
//         _spawnPoints.AddRange(found);
//       }
//       GUILayout.Space(4);

//       EditorGUI.BeginDisabledGroup(true);
//       foreach (var point in _spawnPoints) {
//         EditorGUILayout.ObjectField(point.name, point, typeof(T),  true);
//       }
//       EditorGUI.EndDisabledGroup();

//       EditorGUILayout.LabelField($"{typeof(T).Name}(s): {_spawnPoints.Count}");
//       GUILayout.EndVertical();
//     }
//   }
}