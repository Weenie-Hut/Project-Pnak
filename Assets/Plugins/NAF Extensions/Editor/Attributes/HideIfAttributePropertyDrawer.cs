/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class HideIfAttributePropertyDrawer : PropertyDrawer
	{
		private bool Evaluate(SerializedProperty property, out string error)
		{
			HideIfAttribute attribute = (HideIfAttribute)base.attribute;
			bool result = ExpressionEvaluator.EvaluatePred(property, fieldInfo, attribute.EqualsOrArgs, out error);

			if (attribute.Invert)
				result = !result;

			return result;
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			bool result = Evaluate(property, out string error);

			if (!string.IsNullOrEmpty(error))
				return EditorGUIUtility.singleLineHeight * 2 + EditorGUI.GetPropertyHeight(property, label, true);
			return result ? 0 : EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			bool result = Evaluate(property, out string error);

			if (!string.IsNullOrEmpty(error))
			{
				EditorGUI.HelpBox(position, "Invalid Attribute Usage: " + error, MessageType.Error);
				return;
			}

			if (result)
				return;

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}