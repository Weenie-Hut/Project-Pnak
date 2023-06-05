using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pnak
{
	public static class ExpressionEvaluator
	{
		public static bool EvaluatePredicate(SerializedProperty property, string expression, out string error)
		{
			if (string.IsNullOrEmpty(expression))
			{
				error = "Expression is null or empty.";
				return false;
			}

			if (expression.StartsWith("!"))
			{
				bool result = EvaluatePredicate(property, expression.Substring(1), out error);
				return !result;
			}

			string[] equals = expression.Split("==");
			bool invert = false;
			if (equals.Length == 1)
			{
				equals = expression.Split("!=");
				invert = true;
			}

			if (equals.Length == 2)
			{
				for (int i = 0; i < equals.Length; i++)
					equals[i] = equals[i].Trim();

				SerializedProperty left = property.serializedObject.FindProperty(equals[0]);
				SerializedProperty right = property.serializedObject.FindProperty(equals[1]);

				SerializedPropertyType? expressionType = left?.propertyType ?? right?.propertyType;

				if (expressionType == null)
				{
					error = "Expression Must use at least one property.";
					return false;
				}

				switch(expressionType)
				{
					case SerializedPropertyType.Boolean:
						bool leftBool = StringAsBool(left, equals[0]);
						bool rightBool = StringAsBool(right, equals[1]);
						error = null;
						return !invert ? leftBool == rightBool : leftBool != rightBool;
					case SerializedPropertyType.Integer:
						int leftInt = left?.intValue ?? int.Parse(equals[0]);
						int rightInt = right?.intValue ?? int.Parse(equals[1]);
						error = null;
						return !invert ? leftInt == rightInt : leftInt != rightInt;
					case SerializedPropertyType.Float:
						float leftFloat = left?.floatValue ?? float.Parse(equals[0]);
						float rightFloat = right?.floatValue ?? float.Parse(equals[1]);
						error = null;
						return !invert ? leftFloat == rightFloat : leftFloat != rightFloat;
					case SerializedPropertyType.String:
						string leftString = left?.stringValue ?? equals[0];
						string rightString = right?.stringValue ?? equals[1];
						error = null;
						return !invert ? leftString == rightString : leftString != rightString;
					case SerializedPropertyType.Enum:
						int leftEnum = left?.enumValueIndex ?? int.Parse(equals[0]);
						int rightEnum = right?.enumValueIndex ?? int.Parse(equals[1]);
						error = null;
						return !invert ? leftEnum == rightEnum : leftEnum != rightEnum;
					default:
						error = "Expression type is not supported for equality check.";
						return false;
				}
			}

			SerializedProperty expressionProperty = property.serializedObject.FindProperty(expression);
			if (expressionProperty != null)
			{
				if (expressionProperty.propertyType == SerializedPropertyType.Boolean)
				{
					error = null;
					return expressionProperty.boolValue;
				}
				else if (expressionProperty.propertyType == SerializedPropertyType.ObjectReference)
				{
					error = null;
					return expressionProperty.objectReferenceValue != null;
				}
				else
				{
					error = "Expression is a property, but could not be evaluated as a boolean (must be object reference or boolean).";
					return false;
				}
			}

			error = "Expression could not be evaluated";
			return false;
		}

		public static bool StringAsBool(SerializedProperty property, string value)
		{
			if (property != null)
			{
				if (property.propertyType == SerializedPropertyType.Boolean)
				{
					return property.boolValue;
				}
				else if (property.propertyType == SerializedPropertyType.ObjectReference)
				{
					return property.objectReferenceValue != null;
				}
			}

			if (bool.TryParse(value, out bool result))
				return result;

			return false;
		}

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