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

namespace PnakEditor
{
	internal sealed class ObjectFieldDrawer
	{
		private static class Styles
		{
			private static GUIStyle regularLabelStyle;
			private static GUIStyle selectedLabelStyle;

			public static GUIStyle RegularLabelStyle => regularLabelStyle ??= new GUIStyle(EditorStyles.label);

			public static GUIStyle SelectedLabelStyle
			{
				get
				{
					if (selectedLabelStyle == null)
					{
						selectedLabelStyle = new GUIStyle(EditorStyles.label)
						{
							normal =
						{
							textColor = EditorGUIUtility.isProSkin
								? new Color32(128, 179, 253, 255)
								: new Color32(18, 73, 142, 255)
						},
							hover =
						{
							textColor = EditorGUIUtility.isProSkin
								? new Color32(128, 179, 253, 255)
								: new Color32(18, 73, 142, 255)
						}
						};
					}

					return selectedLabelStyle;
				}
			}
		}

		public delegate void ButtonClickedDelegate(Rect position);

		public delegate void ClickedDelegate();

		public delegate void DeletePressedDelegate();

		public delegate void PropertiesClickedDelegate();

		public delegate void DragAndDropCompletedDelegate(UnityEngine.Object obj);

		private bool isSelected;

		private Event Event => Event.current;

		public event ButtonClickedDelegate ButtonClicked = delegate { };
		public event ClickedDelegate Clicked = delegate { };
		public event DeletePressedDelegate DeletePressed = delegate { };
		public event PropertiesClickedDelegate PropertiesClicked = delegate { };
		public event DragAndDropCompletedDelegate DragAndDropCompleted = delegate { };

		public Func<UnityEngine.Object, bool> DragAndDropFilter { get; set; }

		public ObjectFieldDrawer(Func<UnityEngine.Object, bool> dragAndDropFilter)
		{
			DragAndDropFilter = dragAndDropFilter;
		}

		public void OnGUI(Rect position, GUIContent label, GUIContent content)
		{
			Rect positionWithoutThumb = new Rect(position);
			positionWithoutThumb.xMax -= 20;

			position = DrawPrefixLabel(position, label);
			DrawObjectField(position, content);
			DrawButton(position);

			HandleMouseDown(position, positionWithoutThumb);
			HandleKeyDown();
			HandleDragAndDrop(position);
		}

		private Rect DrawPrefixLabel(Rect position, GUIContent label)
		{
			GUIStyle labelStyle = isSelected ? Styles.SelectedLabelStyle : Styles.RegularLabelStyle;
			Rect result = EditorGUI.PrefixLabel(position, label, labelStyle);
			return result;
		}

		private void DrawObjectField(Rect position, GUIContent objectFieldContent)
		{
			Rect positionWithoutThumb = new Rect(position);
			positionWithoutThumb.xMax -= 20;

			if (Event.type == EventType.Repaint)
			{
				EditorStyles.objectField.Draw(position,
					objectFieldContent,
					position.Contains(Event.mousePosition),
					false,
					false,
					isSelected);
			}
		}

		private void ForceRepaintEditors()
		{
			foreach (UnityEditor.Editor activeEditor in ActiveEditorTracker.sharedTracker.activeEditors)
			{
				activeEditor.Repaint();
			}
		}

		private void DrawButton(Rect position)
		{
			Rect buttonRect = new Rect(position);
			buttonRect.yMin += 1;
			buttonRect.yMax -= 1;
			buttonRect.xMin = buttonRect.xMax - 20;
			buttonRect.xMax -= 1;

			if (GUI.Button(buttonRect, string.Empty, "objectFieldButton"))
			{
				ButtonClicked?.Invoke(position);
			}
		}

		private void HandleMouseDown(Rect position, Rect positionWithoutThumb)
		{
			if (Event.type != EventType.MouseDown)
				return;

			if (Event.button == 0)
			{
				isSelected = positionWithoutThumb.Contains(Event.mousePosition);
				ForceRepaintEditors();
				Clicked?.Invoke();
			}
			else if (Event.button == 1 && positionWithoutThumb.Contains(Event.mousePosition))
			{
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Clear"), false, () => { DeletePressed?.Invoke(); });
				menu.AddItem(new GUIContent("Properties..."), false, () => { PropertiesClicked?.Invoke(); });
				menu.DropDown(position);
				Event.Use();
			}
		}

		private void HandleKeyDown()
		{
			if (!isSelected)
				return;

			if (Event.type == EventType.KeyDown && Event.keyCode == KeyCode.Delete)
			{
				DeletePressed?.Invoke();
			}
		}

		internal static void Show(UnityEngine.Object obj)
		{
			Debug.LogWarning("Show is not implemented yet");
			// Type? propertyEditor = typeof(EditorWindow).Assembly.GetTypes()
			// 	.FirstOrDefault(x => x.Name == "PropertyEditor");

			// if (propertyEditor == null)
			// 	return;

			// MethodInfo? openPropertyEditorMethod = propertyEditor.GetMethod("OpenPropertyEditor",
			// 	BindingFlags.Static | BindingFlags.NonPublic,
			// 	null,
			// 	new Type[]
			// 	{
			// 	typeof(UnityEngine.Object),
			// 	typeof(bool)
			// 	},
			// 	null);

			// if (openPropertyEditorMethod == null)
			// 	return;

			// openPropertyEditorMethod.Invoke(null,
			// 	new object[]
			// 	{
			// 	obj, true
			// 	});
		}

		private void HandleDragAndDrop(Rect position)
		{
			if (!position.Contains(Event.current.mousePosition))
				return;

			if (Event.current.type == EventType.DragPerform)
			{
				HandleDragPerform();
			}
			else
			{
				HandleDragUpdated();
			}
		}

		private void SetDragAndDropMode(bool success)
		{
			// Debug.Log("SetDragAndDropMode: " + success);
			if (success)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}
			else
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
		}

		private void HandleDragUpdated()
		{
			if (DragAndDrop.objectReferences.Length != 1 || DragAndDropFilter == null)
			{
				SetDragAndDropMode(false);
				return;
			}

			UnityEngine.Object objectReference = DragAndDrop.objectReferences[0];
			SetDragAndDropMode(DragAndDropFilter.Invoke(objectReference));
		}

		private void HandleDragPerform()
		{
			DragAndDropCompleted?.Invoke(DragAndDrop.objectReferences[0]);
		}
	}
}