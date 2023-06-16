/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace PnakEditor
{
	internal sealed class UnityObjectPicker : EditorWindow
	{
		private static UnityObjectPicker instance;

		private static UnityObjectPicker Instance
		{
			get
			{
				if (instance == null)
				{
					instance = CreateInstance<UnityObjectPicker>();
				}

				return instance;
			}
		}

		// Dictionary< tab/title, objects >
		private static Dictionary<string, ObjectPickerEntry[]> objectOptions;
		private static Action<UnityEngine.Object> callback;
		private static string searchString = "";
		private static List<ObjectPickerEntry> filteredObjects = new List<ObjectPickerEntry>();
		private static Vector2 scrollPosition = Vector2.zero;

		private static int selectedTab = 0;

		public static void Show(SerializedProperty property, Predicate<GameObject> filter, bool assetsOnly)
		{
			var options = ObjectPickerEntry.CreateObjectPickerDictionary(
				property, filter,
				assetsOnly ? ObjectPickerEntry.IncludeDropdowns.Assets : ObjectPickerEntry.IncludeDropdowns.All);

			UnityObjectPicker.Show(options, (obj) =>
			{
				property.objectReferenceValue = obj;
				property.serializedObject.ApplyModifiedProperties();
			});
		}

		public static void Show(Dictionary<string, ObjectPickerEntry[]> objectOptions, Action<UnityEngine.Object> callback)
		{
			UnityObjectPicker.objectOptions = objectOptions;
			UnityObjectPicker.callback = callback;
			searchString = "";
			filteredObjects.Clear();
			scrollPosition = Vector2.zero;

			Instance.ShowUtility();
		}

		private void OnLostFocus()
		{
			Close();
		}

		private void OnGUI()
		{
			if (objectOptions == null || callback == null)
			{
				Close();
				return;
			}

			// Search bar
			EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
			string newSearchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
			if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
			{
				searchString = "";
				GUI.FocusControl(null);
			}
			else
			{
				if (newSearchString != searchString)
				{
					searchString = newSearchString;
					filteredObjects.Clear();
				}
			}
			GUILayout.EndHorizontal();

			// Tabs
			EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
			string[] tabNames = new string[objectOptions.Count + 1];
			tabNames[0] = "All";
			objectOptions.Keys.CopyTo(tabNames, 1);
			int newSelectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUI.skin.FindStyle("ToolbarButton"));
			if (newSelectedTab != selectedTab)
			{
				selectedTab = newSelectedTab;
				filteredObjects.Clear();
			}
			GUILayout.EndHorizontal();

			// Object list
			if (filteredObjects.Count == 0)
			{
				filteredObjects.Add(new ObjectPickerEntry("None", null));

				if (selectedTab == 0)
				{
					foreach (ObjectPickerEntry[] objects in objectOptions.Values)
					{
						foreach (ObjectPickerEntry obj in objects)
						{
							if (obj.Item1.Contains(searchString))
							{
								filteredObjects.Add(new ObjectPickerEntry(obj.Item1, obj.Item2));
							}
						}
					}
				}
				else
				{
					foreach (ObjectPickerEntry obj in objectOptions[tabNames[selectedTab]])
					{
						if (obj.Item1.Contains(searchString))
						{
							filteredObjects.Add(new ObjectPickerEntry(obj.Item1, obj.Item2));
						}
					}
				}
			}

			// Draw objects in a scroll view with a list layout
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			foreach (ObjectPickerEntry obj in filteredObjects)
			{
				Texture2D preview = AssetPreview.GetAssetPreview(obj.Item2) ??
					AssetPreview.GetMiniThumbnail(obj.Item2) ??
					AssetPreview.GetMiniTypeThumbnail(obj.Item2?.GetType()) ??
					AssetPreview.GetMiniTypeThumbnail(typeof(UnityEngine.Object));

				// Show the preview image to the left of the object name, with the whole row being clickable
				EditorGUILayout.BeginHorizontal();
				GUILayout.Box(preview, GUILayout.Width(26), GUILayout.Height(26));
				if (GUILayout.Button(obj.Item1, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(26)))
				{
					callback(obj.Item2);
					Close();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();

			// Close button and scale slider footer
			EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
			if (GUILayout.Button("Close"))
			{
				Close();
			}
			EditorGUILayout.EndHorizontal();

			// Repaint window to update preview images
			Repaint();

			// Handle keyboard input
			if (Event.current.type == EventType.KeyDown)
			{
				if (Event.current.keyCode == KeyCode.Return)
				{
					if (filteredObjects.Count > 0)
					{
						callback(filteredObjects[0].Item2);
						Close();
					}
				}
				else if (Event.current.keyCode == KeyCode.Escape)
				{
					Close();
				}
			}
		}

		private void OnEnable()
		{
			titleContent = new GUIContent("Select Object");
			minSize = new Vector2(300, 300);
		}

		private void OnDisable()
		{
			objectOptions = null;
			callback = null;
			searchString = "";
			filteredObjects.Clear();
			scrollPosition = Vector2.zero;
		}
	}
}
