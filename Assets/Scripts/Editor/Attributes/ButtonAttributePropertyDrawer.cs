using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(ButtonAttribute))]
	public class ButtonAttributePropertyDrawer : PropertyDrawer
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
			ButtonAttribute buttonAttribute = attribute as ButtonAttribute;

			bool expResult = ExpressionEvaluator.EvaluatePredicate(property, buttonAttribute.HideWhen, out string hideError);
			if (expResult)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}

			if (!string.IsNullOrEmpty(hideError))
			{
				EditorGUI.HelpBox(position, hideError, MessageType.Error);
				return;
			}
			
			Object target = property.serializedObject.targetObject;
			System.Type type = target.GetType();
			System.Reflection.MethodInfo method = type.GetMethod(buttonAttribute.MethodName);

			if (method == null)
			{
				EditorGUI.HelpBox(position, "Method is not valid", MessageType.Error);
				return;
			}

			string buttonName = string.IsNullOrEmpty(buttonAttribute.ButtonName) ?
				ObjectNames.NicifyVariableName(buttonAttribute.MethodName) :
				buttonAttribute.ButtonName;

			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			GUIContent buttonContent = new GUIContent(buttonName);
			GUIStyle buttonStyle = EditorStyles.miniButton;
			Vector2 textDimensions = buttonStyle.CalcSize(buttonContent);

			float width = position.width;
			position.x += position.width - textDimensions.x;
			position.width = textDimensions.x;

			Rect buttonPosition = position;

			position.x -= width - textDimensions.x;
			position.width = width - textDimensions.x;

			EditorGUI.indentLevel = indentLevel;

			EditorGUI.PropertyField(position, property, label, true);
			
			bool GUIEnabled = GUI.enabled;
			GUI.enabled = true;
			if (GUI.Button(buttonPosition, buttonContent, buttonStyle))
			{
				method.Invoke(target, null);
			}
			GUI.enabled = GUIEnabled;
		}
	}
}