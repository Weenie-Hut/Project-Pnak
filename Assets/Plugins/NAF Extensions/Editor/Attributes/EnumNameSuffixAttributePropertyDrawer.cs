/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(EnumNameSuffixAttribute))]
	public class EnumNameSuffixAttributePropertyDrawer : PropertyDrawer
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
			EnumNameSuffixAttribute enumSuffix = attribute as EnumNameSuffixAttribute;

			string[] enumNames = Enum.GetNames(enumSuffix.EnumType);
			int[] enumValues = Enum.GetValues(enumSuffix.EnumType) as int[];

			int propertyValue;
			if (property.propertyType == SerializedPropertyType.Enum)
			{
				propertyValue = property.enumValueIndex;
			}
			else if (property.propertyType == SerializedPropertyType.Integer)
			{
				propertyValue = property.intValue;
			}
			else if (property.propertyType == SerializedPropertyType.String)
			{
				propertyValue = Array.IndexOf(enumNames, property.stringValue);
			}
			else
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			int indexOfProperty = Array.IndexOf(enumValues, propertyValue);

			if (indexOfProperty < 0)
			{
				indexOfProperty = enumNames.Length;
			}
			enumNames = enumNames.Select(n => ObjectNames.NicifyVariableName(n)).Concat(new string[] { enumSuffix.Fallback }).ToArray();

			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			float widthWithoutLabel = position.width - EditorGUIUtility.labelWidth;
			float thisWidth = widthWithoutLabel / 1.5f;

			float width = position.width;
			position.x += position.width - thisWidth;
			position.width = thisWidth;

			int selection = EditorGUI.Popup(position, indexOfProperty, enumNames);

			if (selection != indexOfProperty)
			{
				if (selection == enumNames.Length - 1)
				{
					if (property.propertyType == SerializedPropertyType.Enum)
					{
						property.enumValueIndex = 0;
					}
					else if (property.propertyType == SerializedPropertyType.Integer)
					{
						int size = Marshal.SizeOf(fieldInfo.FieldType);
						int tryCount = 10;
						while(enumValues.Contains(property.intValue) && tryCount-- > 0)
						{
							property.intValue = UnityEngine.Random.Range(0, (int)Mathf.Pow(2, size * 8) - 1);
						}
					}
					else if (property.propertyType == SerializedPropertyType.String)
					{
						property.stringValue = enumSuffix.Fallback;
					}
				}
				else
				{
					if (property.propertyType == SerializedPropertyType.Enum)
					{
						property.enumValueIndex = enumValues[selection];
					}
					else if (property.propertyType == SerializedPropertyType.Integer)
					{
						property.intValue = enumValues[selection];
					}
					else if (property.propertyType == SerializedPropertyType.String)
					{
						property.stringValue = enumNames[selection];
					}
				}
			}

			position.x -= width - thisWidth;
			position.width = width - thisWidth;

			EditorGUI.indentLevel = indent;

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}