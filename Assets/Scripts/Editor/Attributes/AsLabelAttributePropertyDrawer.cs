using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(AsLabelAttribute))]
	public class AsLabelAttributePropertyDrawer : PropertyDrawer
	{
		private string GetText(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.String)
				return property.stringValue;
			else if (property.propertyType == SerializedPropertyType.Integer)
				return property.intValue.ToString();
			else if (property.propertyType == SerializedPropertyType.Float)
				return property.floatValue.ToString();
			else if (property.propertyType == SerializedPropertyType.Boolean)
				return property.boolValue.ToString();
			else if (property.propertyType == SerializedPropertyType.Enum)
				return property.enumNames[property.enumValueIndex];

			return null;
		}

		private GUIContent GetContent(SerializedProperty property, out GUIStyle style, ref Vector2 size)
		{
			GUIContent content = new GUIContent(GetText(property));
			style = GetGUIStyle();
			
			size = style.CalcSize(content);
			return content;
		}

		private GUIStyle GetGUIStyle()
		{
			AsLabelAttribute asLabelAttribute = attribute as AsLabelAttribute;

			GUIStyle style = new GUIStyle(GUI.skin.label);

			if (asLabelAttribute.Type.HasFlag(LabelType.Bold))
				style.fontStyle = FontStyle.Bold;

			if (asLabelAttribute.Type.HasFlag(LabelType.Italic))
				style.fontStyle = FontStyle.Italic;

			if (asLabelAttribute.Type.HasFlag(LabelType.Mini))
				style.fontSize = 9;

			return style;
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			AsLabelAttribute asLabelAttribute = attribute as AsLabelAttribute;

			Vector2 size = Vector2.zero;
			size.x = EditorGUIUtility.currentViewWidth;
			GetContent(property, out GUIStyle style, ref size);
			return size.y;
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			AsLabelAttribute asLabelAttribute = attribute as AsLabelAttribute;

			Vector2 size = position.width * Vector2.one;
			GUIContent content = GetContent(property, out GUIStyle style, ref size);
			EditorGUI.LabelField(position, content, style);
		}
	}
}