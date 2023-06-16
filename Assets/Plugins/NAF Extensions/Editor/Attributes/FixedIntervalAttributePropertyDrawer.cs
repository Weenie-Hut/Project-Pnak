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
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(FixedIntervalAttribute))]
	public class FixedIntervalAttributePropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			FixedIntervalAttribute fixedIntervalAttribute = attribute as FixedIntervalAttribute;
			if (fixedIntervalAttribute == null)
				return EditorGUIUtility.singleLineHeight;
			if (property.propertyType != SerializedPropertyType.Integer)
				return EditorGUIUtility.singleLineHeight;

			return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			FixedIntervalAttribute fixedIntervalAttribute = attribute as FixedIntervalAttribute;
			if (fixedIntervalAttribute == null)
			{
				EditorGUI.HelpBox(position, "FixedIntervalAttribute is not available!", MessageType.Error);
				return;
			}

			if (property.propertyType != SerializedPropertyType.Integer)
			{
				EditorGUI.HelpBox(position, "FixedIntervalAttribute can only be applied to integer fields.", MessageType.Error);
				return;
			}

			Rect labelPosition = position;
			position.height -= EditorGUIUtility.singleLineHeight;

			EditorGUI.BeginProperty(position, label, property);

			DrawingUtility.GUISuffix(ref position, new GUIContent("ticks"));

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(position, property, label, true);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = Mathf.Max(fixedIntervalAttribute.CanBeZero != null ? 0 : 1, property.intValue);
			}

			EditorGUI.EndProperty();

			string description;
			labelPosition.y += EditorGUIUtility.singleLineHeight;
			labelPosition.height = EditorGUIUtility.singleLineHeight;

			if (property.intValue == 0)
			{
				description = fixedIntervalAttribute.CanBeZero;
			}
			else {
				
				float interval = property.intValue * fixedIntervalAttribute.DeltaTime;
				description = string.Format(fixedIntervalAttribute.DescriptionFormat, interval);
			}

			GUIStyle style = DrawingUtility.GetGUIStyle(LabelType.Italic | LabelType.Right);
			EditorGUI.LabelField(labelPosition, description, style);
		}
	}
}