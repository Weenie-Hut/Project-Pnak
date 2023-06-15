/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(MinMaxAttribute))]
	public class MinMaxAttributePropertyDrawer : PropertyDrawer
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

			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					property.longValue = ClampWhole(property.longValue);
					break;
				case SerializedPropertyType.Float:
					property.floatValue = ClampFloat(property.floatValue);
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = new Vector2(
						ClampFloat(property.vector2Value.x),
						ClampFloat(property.vector2Value.y)
					);
					break;
				case SerializedPropertyType.Vector2Int:
					property.vector2IntValue = new Vector2Int(
						(int)ClampWhole(property.vector2IntValue.x),
						(int)ClampWhole(property.vector2IntValue.y)
					);
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = new Vector3(
						ClampFloat(property.vector3Value.x),
						ClampFloat(property.vector3Value.y),
						ClampFloat(property.vector3Value.z)
					);
					break;
				case SerializedPropertyType.Vector3Int:
					property.vector3IntValue = new Vector3Int(
						(int)ClampWhole(property.vector3IntValue.x),
						(int)ClampWhole(property.vector3IntValue.y),
						(int)ClampWhole(property.vector3IntValue.z)
					);
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = new Vector4(
						ClampFloat(property.vector4Value.x),
						ClampFloat(property.vector4Value.y),
						ClampFloat(property.vector4Value.z),
						ClampFloat(property.vector4Value.w)
					);
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = new Rect(
						ClampFloat(property.rectValue.x),
						ClampFloat(property.rectValue.y),
						ClampFloat(property.rectValue.width),
						ClampFloat(property.rectValue.height)
					);
					break;
				case SerializedPropertyType.RectInt:
					property.rectIntValue = new RectInt(
						(int)ClampWhole(property.rectIntValue.x),
						(int)ClampWhole(property.rectIntValue.y),
						(int)ClampWhole(property.rectIntValue.width),
						(int)ClampWhole(property.rectIntValue.height)
					);
					break;
				default:
					throw new System.NotImplementedException("Unimplemented propertyType " + property.propertyType);
			}

			EditorGUI.PropertyField(position, property, label, true);
		}

		private long ClampWhole(long value)
		{
			MinMaxAttribute minMax = attribute as MinMaxAttribute;
			var min = minMax.Min.AsWholeNumeric();
			var max = minMax.Max.AsWholeNumeric();

			if (value < min)
				value = min;
			else if (value > max)
				value = max;
			return value;
		}

		private float ClampFloat(float value)
		{
			MinMaxAttribute minMax = attribute as MinMaxAttribute;
			return Mathf.Clamp(value, (float)minMax.Min, (float)minMax.Max);
		}
	}
}