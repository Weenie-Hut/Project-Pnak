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

namespace Pnak
{
	[CustomPropertyDrawer(typeof(AsEnumAttribute))]
	public class AsEnumAttributePropertyDrawer : PropertyDrawer
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
			AsEnumAttribute asEnumAttribute = attribute as AsEnumAttribute;
			if (asEnumAttribute.EnumType == null)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			string[] enumNames = Enum.GetNames(asEnumAttribute.EnumType);

			if (property.propertyType == SerializedPropertyType.Enum)
			{
				property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, enumNames);
			}
			else if (property.propertyType == SerializedPropertyType.Integer)
			{
				property.intValue = EditorGUI.Popup(position, label.text, property.intValue, enumNames);
			}
			else if (property.propertyType == SerializedPropertyType.String)
			{
				int index = Array.IndexOf(enumNames, property.stringValue);
				index = EditorGUI.Popup(position, label.text, index, enumNames);
				property.stringValue = enumNames[index];
			}
			else if (property.propertyType == SerializedPropertyType.Boolean)
			{
				int index = property.boolValue ? 1 : 0;
				index = EditorGUI.Popup(position, label.text, index, enumNames);
				property.boolValue = index == 1;
			}
		}
	}
}