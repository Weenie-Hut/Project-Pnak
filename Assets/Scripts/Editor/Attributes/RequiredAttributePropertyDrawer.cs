using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredAttributePropertyDrawer : PropertyDrawer
	{
		public bool IsValid(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				return property.objectReferenceValue != null;
			}

			if (property.propertyType == SerializedPropertyType.String)
			{
				return !string.IsNullOrEmpty(property.stringValue);
			}

			if (property.propertyType == SerializedPropertyType.Integer)
			{
				return property.intValue != 0;
			}

			if (property.propertyType == SerializedPropertyType.Float)
			{
				return property.floatValue != 0;
			}

			if (property.propertyType == SerializedPropertyType.Boolean)
			{
				return property.boolValue;
			}

			if (property.propertyType == SerializedPropertyType.Enum)
			{
				return property.enumValueIndex != 0;
			}

			return true;
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			if (!IsValid(property))
			{
				float width = position.width;
				position.x += position.width - 65f;
				position.width = 65f;
				EditorGUI.HelpBox(position, "Required", MessageType.Error);
				position.x -= width - 65f;
				position.width = width - 65f;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}