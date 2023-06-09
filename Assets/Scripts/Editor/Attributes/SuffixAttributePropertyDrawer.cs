using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(SuffixAttribute))]
	public class SuffixAttributePropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			SuffixAttribute suffixAttribute = attribute as SuffixAttribute;
			string suffix = suffixAttribute?.Suffix;
			string tooltip = suffixAttribute?.Tooltip;

			if (!string.IsNullOrEmpty(suffix))
			{
				GUIContent content = new GUIContent(suffix);
				if (!string.IsNullOrEmpty(tooltip))
					label.tooltip = tooltip;

				GUIStyle style = UnityEditor.EditorStyles.miniLabel;
				Vector2 textDimensions = style.CalcSize(content);

				int indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;

				float width = position.width;
				position.x += position.width - textDimensions.x;
				position.width = textDimensions.x;
				UnityEditor.EditorGUI.LabelField(position, content, style);
				position.x -= width - textDimensions.x;
				position.width = width - textDimensions.x;

				EditorGUI.indentLevel = indent;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}