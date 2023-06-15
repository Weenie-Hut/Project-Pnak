/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(DefaultAttribute))]
	public class DefaultAttributePropertyDrawer : PropertyDrawer
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
			DefaultAttribute defaultAttribute = attribute as DefaultAttribute;

			switch(property.propertyType)
			{
				case SerializedPropertyType.Integer:
					if (property.intValue == default)
					{
						property.intValue = defaultAttribute.iValue;
					}
					break;
				case SerializedPropertyType.Float:
					if (property.floatValue == default)
					{
						property.floatValue = defaultAttribute.fValue;
					}
					break;
				case SerializedPropertyType.Boolean:
					if (property.boolValue == default)
					{
						property.boolValue = defaultAttribute.bValue;
					}
					break;
				case SerializedPropertyType.String:
					if (string.IsNullOrEmpty(property.stringValue))
					{
						property.stringValue = defaultAttribute.sValue;
					}
					break;
				case SerializedPropertyType.Enum:
					if (property.enumValueIndex == default)
					{
						property.enumValueIndex = defaultAttribute.iValue;
					}
					break;
				case SerializedPropertyType.Vector2:
					if (property.vector2Value == default)
					{
						property.vector2Value = defaultAttribute.v2Value;
					}
					break;
				case SerializedPropertyType.Vector3:
					if (property.vector3Value == default)
					{
						property.vector3Value = defaultAttribute.v3Value;
					}
					break;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}