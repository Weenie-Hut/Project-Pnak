using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(StateBehaviourController), true)]
	public class StateBehaviourControllerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			// Get the object:
			StateBehaviourController controller = property.objectReferenceValue as StateBehaviourController;
			int prefabIndex = controller?.PrefabIndex ?? -2;

			SearchableAttributePropertyDrawer.Draw(ref position, property, typeof(StateBehaviourController), assetsOnly: true);

			if (prefabIndex != -2)
			{
				GUIContent content = new GUIContent("ID: " + prefabIndex.ToString());
				GUIStyle style = EditorStyles.miniLabel;
				Vector2 textDimensions = style.CalcSize(content);

				int indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;

				float width = position.width;
				position.x += position.width - textDimensions.x;
				position.width = textDimensions.x;

				EditorGUI.LabelField(position, content, style);

				position.x -= width - textDimensions.x;
				position.width = width - textDimensions.x;

				EditorGUI.indentLevel = indent;
			}

			EditorGUI.PropertyField(position, property, label, true);

			if (prefabIndex == -1)
			{
				// Display a warning if the prefab index is not set. Get the prefered height of the warning box:
				float height = EditorGUIUtility.singleLineHeight * 2;
				EditorGUI.HelpBox(new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, height), "Target has not been added to the Network Prefab pool!", MessageType.Error);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUI.GetPropertyHeight(property, label);

			// Get the object:
			StateBehaviourController controller = property.objectReferenceValue as StateBehaviourController;
			int prefabIndex = controller?.PrefabIndex ?? -2;
			if (prefabIndex == -1)
				height += EditorGUIUtility.singleLineHeight * 2;

			height += SearchableAttributePropertyDrawer.GetExtraHeight(property, assetsOnly: true);

			return height;
		}
	}
}