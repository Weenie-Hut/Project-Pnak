using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfAttributePropertyDrawer : PropertyDrawer
	{
		private bool Evaluate(SerializedProperty property, out string error)
		{
			HideIfAttribute attribute = (HideIfAttribute)base.attribute;
			bool result = ExpressionEvaluator.EvaluatePredicate(property, attribute.Expression, out error);

			return attribute.Invert ? !result : result;
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
				EditorGUI.HelpBox(position, "HideIfAttribute: Expression is invalid: " + error, MessageType.Error);
				return;
			}

			if (result)
				return;

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}