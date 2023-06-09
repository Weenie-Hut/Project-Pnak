using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(ValidateAttribute))]
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class ValidateAttributePropertyDrawer : PropertyDrawer
	{
		private bool Evaluate(SerializedProperty property, out string error)
		{
			ValidateAttribute attribute = (ValidateAttribute)base.attribute;
			return ExpressionEvaluator.EvaluatePred(property, fieldInfo, attribute.EqualsOrArgs, out error);
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			bool result = Evaluate(property, out string error);
			if (!string.IsNullOrEmpty(error))
				return EditorGUIUtility.singleLineHeight * 2 + EditorGUI.GetPropertyHeight(property, label, true);
			return EditorGUI.GetPropertyHeight(property, label, true);
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

			if (!result)
			{
				ValidateAttribute attribute = (ValidateAttribute)base.attribute;

				float width = position.width;
				position.x += position.width - 65f;
				position.width = 65f;
				EditorGUI.HelpBox(position, attribute.Message, MessageType.Error);
				position.x -= width - 65f;
				position.width = width - 65f;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}