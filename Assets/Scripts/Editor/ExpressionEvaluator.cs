using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Pnak;
using UnityEngine;

namespace PnakEditor
{
	public static class ExpressionEvaluator
	{
		public static bool EvaluatePred(SerializedProperty property, FieldInfo fieldInfo, MutliType[] equalsOrArgs, out string error)
		{
			if (equalsOrArgs == null || equalsOrArgs.Length == 0)
			{
				error = null;
				return PropertyIsNotDefault(property, out error);
			}

			Type compareType = null;
			object value = null;

			if (!string.IsNullOrEmpty(equalsOrArgs[0]._string))
			{
				MethodInfo methodInfo = fieldInfo.DeclaringType.GetMethod(equalsOrArgs[0].String, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

				if (methodInfo != null)
				{
					equalsOrArgs = equalsOrArgs.Skip(1).ToArray();
					value = RunMethod(property, methodInfo, ref equalsOrArgs, out error);
					compareType = methodInfo.ReturnType;
				}
			}

			return IsAllEqual(property, equalsOrArgs, compareType, value, out error);
		}

		public static bool IsAllEqual(SerializedProperty property, MutliType[] equalsOrArgs, Type compareType, object value, out string error)
		{
			SerializedProperty[] others = equalsOrArgs.Select(x => string.IsNullOrEmpty(x._string) ? null : property.serializedObject.FindProperty(x._string)).ToArray();

			int skipIndex = -1;

			if (compareType == null || value == null)
			{
				SerializedProperty propertyToMatch = property;
				for (int i = 0; i < others.Length; i++)
				{
					if (others[i] != null)
					{
						propertyToMatch = others[i];
						skipIndex = i;
						break;
					}
				}

				if (!TryGetPropertyType(propertyToMatch, out compareType))
				{
					error = $"Cannot compare {(propertyToMatch).name} because type {(propertyToMatch).propertyType} is not supported.";
					return false;
				}

				if (!TryPropertyAsType(compareType, propertyToMatch, out value))
				{
					throw new Exception($"Property {propertyToMatch.name} has type {compareType} but cannot be converted to {compareType}.");
				}
			}

			object otherValue = null;
			// UnityEngine.Debug.Log($"Compare type: {compareType} Value: {value} => " + equalsOrArgs.Select((e, i) => {
			// 	if (others[i] != null)
			// 	{
			// 		if (TryPropertyAsType(compareType, others[i], out otherValue))
			// 			return e.ToString() + " (" + otherValue + ")";
			// 		else return e.ToString() + " (invalid)";
			// 	}
			// 	return e.ToString();
			// }).Format() + " skip: " + skipIndex + " count: " + others.Length);

			bool result = true;
			int count = 0;
			for (int i = 0; i < others.Length; i++)
			{
				if (i == skipIndex)
				{
					continue;
				}

				if (i + 1 == others.Length && compareType != typeof(bool) && (
					others[i] == null ? 
						(otherValue = equalsOrArgs[i]._bool) != null :
						TryPropertyAsType(typeof(bool), others[i], out otherValue)
				)) {
					if (count == 0)
					{
						result = ObjectIsNotDefault(compareType, value, out error);
						if (error != null) return false;
						count++;
					}

					result = (bool)otherValue == result;
					break;
				}

				if (result == false) continue;
				
				if (others[i] == null)
				{
					if (!equalsOrArgs[i].TryGetType(compareType, out otherValue))
					{
						error = $"Constant argument {i} cannot be converted to type {compareType}.";
						return false;
					}
				}
				else if (!TryPropertyAsType(compareType, others[i], out otherValue))
				{
					error = $"Property {others[i].name} does not match compare type: {compareType}.";
					return false;
				}

				if (!value.Equals(otherValue))
				{
					result = false;
				}
				count++;
			}

			if (count == 0)
			{
				result = ObjectIsNotDefault(compareType, value, out error);
				if (error != null) return false;
			}

			error = null;
			return result;
		}

		public static object RunMethod(SerializedProperty property, MethodInfo methodInfo, ref MutliType[] equalsOrArgs, out string error)
		{
			if (methodInfo == null)
			{
				error = "Method not found.";
				return false;
			}

			ParameterInfo[] parameters = methodInfo.GetParameters();

			if (parameters.Length > equalsOrArgs.Length)
			{
				error = $"Method {methodInfo.Name} has {parameters.Length} parameters but only {equalsOrArgs.Length} were provided. Extra parameters are used for equality, but there are not enough.";
				return false;
			}

			object[] args = new object[parameters.Length];
			int parameterIndex = 0;

			SerializedProperty[] others = equalsOrArgs.Select(x => string.IsNullOrEmpty(x._string) ? null : property.serializedObject.FindProperty(x._string)).ToArray();
			for (int argIndex = 0; argIndex < equalsOrArgs.Length && argIndex < parameters.Length; argIndex++)
			{
				if (others[argIndex] != null)
				{
					if (!TryPropertyAsType(parameters[parameterIndex].ParameterType, others[argIndex], out args[parameterIndex]))
					{
						error = $"Property {others[argIndex].name} doesn't match functions index {parameterIndex} parameter type {parameters[parameterIndex].ParameterType}.";
						return false;
					}
				}
				else if (equalsOrArgs[argIndex].HasAny)
				{
					if (!equalsOrArgs[argIndex].TryGetType(parameters[parameterIndex].ParameterType, out args[parameterIndex]))
					{
						error = $"Cannot use constants with functions index {parameterIndex} parameter type {parameters[parameterIndex].ParameterType}.";
						return false;
					}
				}
				else
				{
					throw new ArgumentException("equalsOrArgs has data with no value.");
				}
				parameterIndex++;
			}

			equalsOrArgs = equalsOrArgs.Skip(parameters.Length).ToArray();

			try {
				error = null;
				return methodInfo.Invoke(methodInfo.IsStatic ? null : property.serializedObject.targetObject, args);
			}
			catch (Exception e)
			{
				error = "Error invoking method: " + e.Message;
				return false;
			}
		}

		public static bool PropertyIsNotDefault(SerializedProperty property, out string error)
		{
			error = null;
			switch (property.propertyType)
			{
				case SerializedPropertyType.Boolean:
					return property.boolValue != default;
				case SerializedPropertyType.Float:
					return property.floatValue != default;
				case SerializedPropertyType.Integer:
					return property.longValue != default;
				case SerializedPropertyType.String:
					return !string.IsNullOrEmpty(property.stringValue);
				case SerializedPropertyType.Enum:
					return property.enumValueIndex != default;
				case SerializedPropertyType.Color:
					return property.colorValue != default;
				case SerializedPropertyType.Vector2:
					return property.vector2Value != default;
				case SerializedPropertyType.Vector3:
					return property.vector3Value != default;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue != default;
				default:
					error = $"Type {property.propertyType} is not supported.";
					return false;
			}
		}

		public static bool ObjectIsNotDefault(Type type, object value, out string error)
		{
			error = null;
			if (type == typeof(string))
			{
				return !string.IsNullOrEmpty((string)value);
			}
			else if (type.IsWholeNumeric())
			{
				return ((long)value.AsWholeNumeric()) != 0;
			}
			else if (type == typeof(float))
			{
				return (float)value != 0.0;
			}
			else if (type == typeof(bool))
			{
				return (bool)value != false;
			}
			else if (type == typeof(UnityEngine.Object))
			{
				return (UnityEngine.Object)value != null;
			}
			else if (type == typeof(Color))
			{
				return (Color)value != default;
			}
			else if (type == typeof(Vector2))
			{
				return (Vector2)value != default;
			}
			else if (type == typeof(Vector3))
			{
				return (Vector3)value != default;
			}
			else
			{
				error = $"Function return type {type} is not supported.";
				return false;
			}
		}

		public static object GetPropertyValue(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Boolean:
					return property.boolValue;
				case SerializedPropertyType.Float:
					return property.floatValue;
				case SerializedPropertyType.Integer:
					return property.longValue;
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.Enum:
					return property.enumValueIndex;
				case SerializedPropertyType.Color:
					return property.colorValue;
				case SerializedPropertyType.Vector2:
					return property.vector2Value;
				case SerializedPropertyType.Vector3:
					return property.vector3Value;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue;
				case SerializedPropertyType.Generic:
					return property.objectReferenceValue;
				case SerializedPropertyType.ArraySize:
					return property.intValue;
					case SerializedPropertyType.Character:
					return property.intValue;
				case SerializedPropertyType.AnimationCurve:
					return property.animationCurveValue;
				case SerializedPropertyType.Bounds:
					return property.boundsValue;
				// case SerializedPropertyType.Gradient:
				// 	return property.value;
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue;
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue;
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize;
				case SerializedPropertyType.Vector4:
					return property.vector4Value;
				case SerializedPropertyType.Rect:
					return property.rectValue;
				case SerializedPropertyType.LayerMask:
					return property.intValue;
				case SerializedPropertyType.RectInt:
					return property.rectIntValue;
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue;
				default:
					throw new ArgumentException("Property type " + property.propertyType + " is not supported.");
			}
		}

		public static bool TryGetPropertyType(SerializedProperty property, out Type type)
		{
			if (property.propertyType == SerializedPropertyType.String)
			{
				type = typeof(string);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Integer)
			{
				type = typeof(long);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Float)
			{
				type = typeof(float);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Boolean)
			{
				type = typeof(bool);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Enum)
			{
				type = typeof(long);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				type = typeof(UnityEngine.Object);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Color)
			{
				type = typeof(Color);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Vector2)
			{
				type = typeof(Vector2);
				return true;
			}
			else if (property.propertyType == SerializedPropertyType.Vector3)
			{
				type = typeof(Vector3);
				return true;
			}

			type = null;
			return false;
		}

		public static bool TryPropertyAsType(Type type, SerializedProperty property, out object result)
		{
			if (type == typeof(string))
			{
				if (property.propertyType == SerializedPropertyType.String)
				{
					result = property.stringValue;
					return true;
				}
			}
			else if (type.IsWholeNumeric())
			{
				if (property.propertyType == SerializedPropertyType.Integer)
				{
					result = property.longValue;
					return true;
				}
				else if (property.propertyType == SerializedPropertyType.Enum)
				{
					result = property.enumValueIndex;
					return true;
				}
			}
			else if (type == typeof(float))
			{
				if (property.propertyType == SerializedPropertyType.Float)
				{
					result = property.floatValue;
					return true;
				}
			}
			else if (type == typeof(bool))
			{
				if (property.propertyType == SerializedPropertyType.Boolean)
				{
					result = property.boolValue;
					return true;
				}
			}
			else if (type == typeof(UnityEngine.Object))
			{
				if (property.propertyType == SerializedPropertyType.ObjectReference)
				{
					result = property.objectReferenceValue;
					return true;
				}
			}
			else if (type == typeof(Color))
			{
				if (property.propertyType == SerializedPropertyType.Color)
				{
					result = property.colorValue;
					return true;
				}
			}
			else if (type == typeof(Vector2))
			{
				if (property.propertyType == SerializedPropertyType.Vector2)
				{
					result = property.vector2Value;
					return true;
				}
			}
			else if (type == typeof(Vector3))
			{
				if (property.propertyType == SerializedPropertyType.Vector3)
				{
					result = property.vector3Value;
					return true;
				}
			}

			result = null;
			return false;
		}
		
		// public static bool EvaluatePredicate(SerializedProperty property, string expression, out string error)
		// {
		// 	if (string.IsNullOrEmpty(expression))
		// 	{
		// 		error = "Expression is null or empty.";
		// 		return false;
		// 	}

		// 	if (expression.StartsWith("!"))
		// 	{
		// 		bool result = EvaluatePredicate(property, expression.Substring(1), out error);
		// 		return !result;
		// 	}

		// 	string[] equals = expression.Split("==");
		// 	bool invert = false;
		// 	if (equals.Length == 1)
		// 	{
		// 		equals = expression.Split("!=");
		// 		invert = true;
		// 	}

		// 	if (equals.Length == 2)
		// 	{
		// 		for (int i = 0; i < equals.Length; i++)
		// 			equals[i] = equals[i].Trim();

		// 		SerializedProperty left = property.serializedObject.FindProperty(equals[0]);
		// 		SerializedProperty right = property.serializedObject.FindProperty(equals[1]);

		// 		SerializedPropertyType? expressionType = left?.propertyType ?? right?.propertyType;

		// 		if (expressionType == null)
		// 		{
		// 			error = "Expression Must use at least one property.";
		// 			return false;
		// 		}

		// 		switch(expressionType)
		// 		{
		// 			case SerializedPropertyType.Boolean:
		// 				bool leftBool = StringAsBool(left, equals[0]);
		// 				bool rightBool = StringAsBool(right, equals[1]);
		// 				error = null;
		// 				return !invert ? leftBool == rightBool : leftBool != rightBool;
		// 			case SerializedPropertyType.Integer:
		// 				int leftInt = left?.intValue ?? int.Parse(equals[0]);
		// 				int rightInt = right?.intValue ?? int.Parse(equals[1]);
		// 				error = null;
		// 				return !invert ? leftInt == rightInt : leftInt != rightInt;
		// 			case SerializedPropertyType.Float:
		// 				float leftFloat = left?.floatValue ?? float.Parse(equals[0]);
		// 				float rightFloat = right?.floatValue ?? float.Parse(equals[1]);
		// 				error = null;
		// 				return !invert ? leftFloat == rightFloat : leftFloat != rightFloat;
		// 			case SerializedPropertyType.String:
		// 				string leftString = left?.stringValue ?? equals[0];
		// 				string rightString = right?.stringValue ?? equals[1];
		// 				error = null;
		// 				return !invert ? leftString == rightString : leftString != rightString;
		// 			case SerializedPropertyType.Enum:
		// 				int leftEnum = left?.enumValueIndex ?? int.Parse(equals[0]);
		// 				int rightEnum = right?.enumValueIndex ?? int.Parse(equals[1]);
		// 				error = null;
		// 				return !invert ? leftEnum == rightEnum : leftEnum != rightEnum;
		// 			default:
		// 				error = "Expression type is not supported for equality check.";
		// 				return false;
		// 		}
		// 	}

		// 	SerializedProperty expressionProperty = property.serializedObject.FindProperty(expression);
		// 	if (expressionProperty != null)
		// 	{
		// 		if (expressionProperty.propertyType == SerializedPropertyType.Boolean)
		// 		{
		// 			error = null;
		// 			return expressionProperty.boolValue;
		// 		}
		// 		else if (expressionProperty.propertyType == SerializedPropertyType.ObjectReference)
		// 		{
		// 			error = null;
		// 			return expressionProperty.objectReferenceValue != null;
		// 		}
		// 		else
		// 		{
		// 			error = "Expression is a property, but could not be evaluated as a boolean (must be object reference or boolean).";
		// 			return false;
		// 		}
		// 	}

		// 	error = "Expression could not be evaluated";
		// 	return false;
		// }

		// public static bool StringAsBool(SerializedProperty property, string value)
		// {
		// 	if (property != null)
		// 	{
		// 		if (property.propertyType == SerializedPropertyType.Boolean)
		// 		{
		// 			return property.boolValue;
		// 		}
		// 		else if (property.propertyType == SerializedPropertyType.ObjectReference)
		// 		{
		// 			return property.objectReferenceValue != null;
		// 		}
		// 	}

		// 	if (bool.TryParse(value, out bool result))
		// 		return result;

		// 	return false;
		// }

		// public class Parser
		// {
		// 	// Formats:
		// 	// "property"
		// 	// "!property"
		// 	// "property == value"
		// 	// "property != value"
		// 	// "property > value"
		// 	// "property < value"
		// 	// "property >= value"
		// 	// "property <= value"
		// 	// "Format && Format"
		// 	// "Format || Format"
		// 	// "(Format)"
		// 	// "!(Format)"

		// 	private string _expression;
		// 	private SerializedProperty _property;
		// 	private List<Token> tokens;

		// 	public class Token
		// 	{
		// 		public enum Type
		// 		{
		// 			Property,
		// 			Equals,
		// 			NotEquals,
		// 			GreaterThan,
		// 			LessThan,
		// 			GreaterThanOrEquals,
		// 			LessThanOrEquals,
		// 			String,
		// 			Int,
		// 			Float,
		// 			Boolean,
		// 			And,
		// 			Or,
		// 			Not,
		// 			OpenParenthesis,
		// 			CloseParenthesis,
		// 		}

		// 		public Type type;
		// 		public string value;
		// 	}

		// 	public Parser(SerializedProperty property, string expression)
		// 	{
		// 		_expression = expression;
		// 		_property	= property;

		// 		// Tokenize
		// 		tokens = new List<Token>();
		// 		int index = 0;
		// 		while (index < _expression.Length)
		// 		{
		// 			char c = _expression[index];
		// 			if (char.IsWhiteSpace(c))
		// 			{
		// 				index++;
		// 				continue;
		// 			}

		// 			if (c == '(')
		// 			{
		// 				tokens.Add(new Token { type = Token.Type.OpenParenthesis });
		// 				index++;
		// 				continue;
		// 			}

		// 			if (c == ')')
		// 			{
		// 				tokens.Add(new Token { type = Token.Type.CloseParenthesis });
		// 				index++;
		// 				continue;
		// 			}

		// 			if (c == '!')
		// 			{
		// 				tokens.Add(new Token { type = Token.Type.Not });
		// 				index++;
		// 				continue;
		// 			}

		// 			if (c == '&')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '&')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.And });
		// 					index += 2;
		// 					continue;
		// 				}
		// 			}

		// 			if (c == '|')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '|')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.Or });
		// 					index += 2;
		// 					continue;
		// 				}
		// 			}

		// 			if (char.IsLetter(c))
		// 			{
		// 				int start = index;
		// 				while (index < _expression.Length && (char.IsLetterOrDigit(_expression[index]) || _expression[index] == '_'))
		// 				{
		// 					index++;
		// 				}
		// 				string value = _expression.Substring(start, index - start);

		// 				if (value == "true" || value == "false")
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.Boolean, value = value });
		// 				}
		// 				else if (property.serializedObject.FindProperty(value) != null)
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.Property, value = value });
		// 				}
		// 				else tokens.Add(new Token { type = Token.Type.String, value = value });

		// 				continue;
		// 			}

		// 			if (char.IsDigit(c))
		// 			{
		// 				int start = index;
		// 				bool isFloat = false;
		// 				while (index < _expression.Length && (char.IsDigit(_expression[index]) || _expression[index] == '.'))
		// 				{
		// 					if (_expression[index] == '.')
		// 					{
		// 						if (isFloat)
		// 						{
		// 							throw new Exception("Invalid number format.");
		// 						}
		// 						isFloat = true;
		// 					}
		// 					index++;
		// 				}
		// 				string value = _expression.Substring(start, index - start);
		// 				if (isFloat)
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.Float, value = value });
		// 				}
		// 				else tokens.Add(new Token { type = Token.Type.Int, value = value });

		// 				continue;
		// 			}

		// 			if (c == '=')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '=')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.Equals, value = "==" });
		// 					index += 2;
		// 					continue;
		// 				}
		// 			}

		// 			if (c == '!')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '=')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.NotEquals, value = "!=" });
		// 					index += 2;
		// 					continue;
		// 				}
		// 			}

		// 			if (c == '<')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '=')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.LessThanOrEquals, value = "<=" });
		// 					index += 2;
		// 					continue;
		// 				}
		// 				else
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.LessThan, value = "<" });
		// 					index++;
		// 					continue;
		// 				}
		// 			}

		// 			if (c == '>')
		// 			{
		// 				if (index + 1 < _expression.Length && _expression[index + 1] == '=')
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.GreaterThanOrEquals, value = ">=" });
		// 					index += 2;
		// 					continue;
		// 				}
		// 				else
		// 				{
		// 					tokens.Add(new Token { type = Token.Type.GreaterThan, value = ">" });
		// 					index++;
		// 					continue;
		// 				}
		// 			}

		// 			if (c == ' ')
		// 			{
		// 				index++;
		// 				continue;
		// 			}

		// 			Debug.LogError("Invalid character in expression: " + c);
		// 			index++;
		// 		}
		// 	}
		
		// 	public bool Evaluate()
		// 	{
		// 		Stack<string> valueStack = new Stack<string>();
		// 		Stack<Token> operatorStack = new Stack<Token>();
		// 	}
		// }
	}
}