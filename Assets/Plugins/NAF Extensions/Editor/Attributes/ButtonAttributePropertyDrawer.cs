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
using System.Reflection;
using System.Collections.Generic;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(ButtonAttribute))]
	[CustomPropertyDrawer(typeof(NaNButtonAttribute))]
	public class ButtonAttributePropertyDrawer : PropertyDrawer
	{
		private bool Evaluate(SerializedProperty property, out string error)
		{
			ButtonAttribute attribute = (ButtonAttribute)base.attribute;

			if (attribute.HideWhen == null || attribute.HideWhen.Length == 0)
			{
				error = null;
				return false;
			}

			return ExpressionEvaluator.EvaluatePred(property, fieldInfo, attribute.HideWhen, out error);
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
			ButtonAttribute buttonAttribute = attribute as ButtonAttribute;

			// UnityEngine.Debug.Log("Button: { " + buttonAttribute.MethodName + ", " + buttonAttribute.ButtonName + ", " + buttonAttribute.Tooltip + ", " + buttonAttribute.HideWhen.Format() + " }");

			bool result = Evaluate(property, out string error);
			if (!string.IsNullOrEmpty(error))
			{
				EditorGUI.HelpBox(position, "Invalid Attribute Usage: " + error, MessageType.Error);
				return;
			}

			if (result)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}
			
			Object target = property.serializedObject.targetObject;
			System.Type type = target.GetType();
			MethodInfo method = type.GetMethod(buttonAttribute.MethodName);

			if (method == null && !DefaultMethods.ContainsKey(buttonAttribute.MethodName))
			{
				EditorGUI.HelpBox(position, $"Method {buttonAttribute.MethodName} is not valid", MessageType.Error);
				return;
			}

			string buttonName = string.IsNullOrEmpty(buttonAttribute.ButtonName) ?
				ObjectNames.NicifyVariableName(buttonAttribute.MethodName) :
				buttonAttribute.ButtonName;

			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			GUIContent buttonContent = new GUIContent(buttonName, buttonAttribute.Tooltip);

			GUIStyle buttonStyle = EditorStyles.miniButton;
			Vector2 textDimensions = buttonStyle.CalcSize(buttonContent);

			float width = position.width;
			position.x += position.width - textDimensions.x;
			position.width = textDimensions.x;

			Rect buttonPosition = position;

			bool GUIEnabled = GUI.enabled;
			GUI.enabled = true;

			if (GUI.Button(buttonPosition, buttonContent, buttonStyle))
			{
				if (method == null)
				{
					DefaultMethods[buttonAttribute.MethodName](property);
				}
				else
				{
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length == 1)
					{
						if (parameters[0].ParameterType == typeof(SerializedProperty))
							method.Invoke(target, new object[] { property });
						else if (ExpressionEvaluator.TryPropertyAsType(parameters[0].ParameterType, property, out object arg))
							method.Invoke(target, new object[] { arg });
						else if (parameters[0].ParameterType == typeof(Object))
							method.Invoke(target, new object[] { target });
						else
							Debug.LogError("Invalid parameter type for method " + method.Name);
					}
					else if (parameters.Length == 0)
						method.Invoke(target, null);
					else Debug.LogError("Invalid number of parameters for method " + method.Name);
				}
			}
			GUI.enabled = GUIEnabled;

			position.x -= width - textDimensions.x;
			position.width = width - textDimensions.x;

			EditorGUI.indentLevel = indentLevel;

			EditorGUI.PropertyField(position, property, label, true);
			
			
		}

		public static readonly Dictionary<string, System.Action<SerializedProperty>> DefaultMethods = new Dictionary<string, System.Action<SerializedProperty>>()
		{
			{ "SetNAN", SetFloatToNaN },
			{ "SetMaxValue", SetMaxValue },
			{ "SetMinValue", SetMinValue },
			{ "SetPosInf", SetFloatToPosInf },
			{ "SetNegInf", SetFloatToNegInf },
		};

		public static void SetFloatToNaN(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.Float)
				property.floatValue = float.NaN;
			else if (property.propertyType == SerializedPropertyType.Vector2)
				property.vector2Value = new Vector2(float.NaN, float.NaN);
			else if (property.propertyType == SerializedPropertyType.Vector3)
				property.vector3Value = new Vector3(float.NaN, float.NaN, float.NaN);
			else if (property.propertyType == SerializedPropertyType.Vector4)
				property.vector4Value = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
			else if (property.propertyType == SerializedPropertyType.Quaternion)
				property.quaternionValue = new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN);
			else if (property.propertyType == SerializedPropertyType.Rect)
				property.rectValue = new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
			
			else {
				Debug.LogError("Invalid property type for method SetNAN");
			}
		}

		public static void SetMaxValue(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.Integer)
			{
				FieldInfo fieldInfo = property.serializedObject.targetObject.GetType().GetField(property.propertyPath);
				if (fieldInfo.FieldType == typeof(byte))
					property.intValue = byte.MaxValue;
				else if (fieldInfo.FieldType == typeof(sbyte))
					property.intValue = sbyte.MaxValue;
				else if (fieldInfo.FieldType == typeof(short))
					property.intValue = short.MaxValue;
				else if (fieldInfo.FieldType == typeof(ushort))
					property.intValue = ushort.MaxValue;
				else if (fieldInfo.FieldType == typeof(int))
					property.intValue = int.MaxValue;
				else if (fieldInfo.FieldType == typeof(uint))
					property.longValue = uint.MaxValue;
				else if (fieldInfo.FieldType == typeof(long))
					property.longValue = long.MaxValue;
				else
					Debug.LogError("Invalid field type for method SetMaxValue");
			}
			else if (property.propertyType == SerializedPropertyType.Float)
				property.floatValue = float.MaxValue;
			else if (property.propertyType == SerializedPropertyType.Vector2)
				property.vector2Value = new Vector2(float.MaxValue, float.MaxValue);
			else if (property.propertyType == SerializedPropertyType.Vector3)
				property.vector3Value = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			else if (property.propertyType == SerializedPropertyType.Vector4)
				property.vector4Value = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
			else if (property.propertyType == SerializedPropertyType.Quaternion)
				property.quaternionValue = new Quaternion(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
			else if (property.propertyType == SerializedPropertyType.Rect)
				property.rectValue = new Rect(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
			else {
				Debug.LogError("Invalid property type for method SetMaxValue");
			}
		}

		public static void SetMinValue(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.Integer)
			{
				FieldInfo fieldInfo = property.serializedObject.targetObject.GetType().GetField(property.propertyPath);
				if (fieldInfo.FieldType == typeof(byte))
					property.intValue = byte.MinValue;
				else if (fieldInfo.FieldType == typeof(sbyte))
					property.intValue = sbyte.MinValue;
				else if (fieldInfo.FieldType == typeof(short))
					property.intValue = short.MinValue;
				else if (fieldInfo.FieldType == typeof(ushort))
					property.intValue = ushort.MinValue;
				else if (fieldInfo.FieldType == typeof(int))
					property.intValue = int.MinValue;
				else if (fieldInfo.FieldType == typeof(uint))
					property.longValue = uint.MinValue;
				else if (fieldInfo.FieldType == typeof(long))
					property.longValue = long.MinValue;
				else
					Debug.LogError("Invalid field type for method SetMinValue");
			}
			else if (property.propertyType == SerializedPropertyType.Float)
				property.floatValue = float.MinValue;
			else if (property.propertyType == SerializedPropertyType.Vector2)
				property.vector2Value = new Vector2(float.MinValue, float.MinValue);
			else if (property.propertyType == SerializedPropertyType.Vector3)
				property.vector3Value = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			else if (property.propertyType == SerializedPropertyType.Vector4)
				property.vector4Value = new Vector4(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
			else if (property.propertyType == SerializedPropertyType.Quaternion)
				property.quaternionValue = new Quaternion(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
			else if (property.propertyType == SerializedPropertyType.Rect)
				property.rectValue = new Rect(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
			else {
				Debug.LogError("Invalid property type for method SetMinValue");
			}
		}

		public static void SetFloatToPosInf(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.Float)
				property.floatValue = float.PositiveInfinity;
			else if (property.propertyType == SerializedPropertyType.Vector2)
				property.vector2Value = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			else if (property.propertyType == SerializedPropertyType.Vector3)
				property.vector3Value = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			else if (property.propertyType == SerializedPropertyType.Vector4)
				property.vector4Value = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			else if (property.propertyType == SerializedPropertyType.Quaternion)
				property.quaternionValue = new Quaternion(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			else if (property.propertyType == SerializedPropertyType.Rect)
				property.rectValue = new Rect(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			else {
				Debug.LogError("Invalid property type for method SetPosInf");
			}
		}

		public static void SetFloatToNegInf(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.Float)
				property.floatValue = float.NegativeInfinity;
			else if (property.propertyType == SerializedPropertyType.Vector2)
				property.vector2Value = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
			else if (property.propertyType == SerializedPropertyType.Vector3)
				property.vector3Value = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			else if (property.propertyType == SerializedPropertyType.Vector4)
				property.vector4Value = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			else if (property.propertyType == SerializedPropertyType.Quaternion)
				property.quaternionValue = new Quaternion(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			else if (property.propertyType == SerializedPropertyType.Rect)
				property.rectValue = new Rect(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
			else {
				Debug.LogError("Invalid property type for method SetNegInf");
			}
		}
	}
}