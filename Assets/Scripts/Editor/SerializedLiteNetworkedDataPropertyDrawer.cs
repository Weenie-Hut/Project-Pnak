using UnityEditor;
using Pnak;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Fusion;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(SerializedLiteNetworkedData))]
	public class SerializedLiteNetworkedDataPropertyDrawer : PropertyDrawer
	{
		private SerializedProperty scriptTypeProperty;
		private SerializedProperty customDataProperty_hidden;
		private SerializedProperty customDataProperty_Drawn;

		private void SetProperties(SerializedProperty property)
		{
			scriptTypeProperty = property.FindPropertyRelative("scriptType");
			customDataProperty_hidden = property.FindPropertyRelative("CustomData");

			if (scriptTypeProperty == null)
				UnityEngine.Debug.LogWarning("LiteNetworkedDataPropertyDrawer: scriptTypeProperty is null");

			SetCustomDataProperties();
		}

		private void SetCustomDataProperties()
		{
			string customDataPropertyName = null;
			if (scriptTypeProperty.intValue < LiteNetworkModScripts.ModOptions.Length)
				customDataPropertyName = LiteNetworkModScripts.FieldNames[scriptTypeProperty.intValue];

			if (customDataPropertyName == null)
				customDataProperty_Drawn = null;
			else customDataProperty_Drawn = LiteNetworkedSO.SerializedInstance.FindProperty(customDataPropertyName);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			SetProperties(property);

			int scriptIndex = scriptTypeProperty.intValue;

			string customName = property.displayName;
			// customDataProperty = property.FindPropertyRelative(customDataPropertyNames[scriptIndex]);

			Rect scriptTypePosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			if (scriptIndex >= LiteNetworkModScripts.ModOptions.Length)
			{
				scriptIndex = scriptTypeProperty.intValue = 0;
			}

			scriptIndex = EditorGUI.Popup(scriptTypePosition, scriptIndex, LiteNetworkModScripts.ModOptions);

			if (scriptIndex != scriptTypeProperty.intValue)
			{
				scriptTypeProperty.intValue = scriptIndex;
				SetCustomDataProperties();

				if (customDataProperty_Drawn != null)
				{
					LiteNetworkedData defaultData = default;
					LiteNetworkModScripts.Instance.Mods[scriptIndex - 1].SetDefaults(ref defaultData);
					UnityEngine.Debug.Log(defaultData.ToString());
					SetByteBuffer(customDataProperty_hidden, defaultData.SafeCustomData);
				}
			}
			

			if (customDataProperty_Drawn != null)
			{
				System.Diagnostics.Debug.Assert(customDataProperty_hidden.isFixedBuffer);
				byte[] customData = GetByteBuffer(customDataProperty_hidden);
				SetAllFromRawData(customDataProperty_Drawn, customData);

				Rect customDataPosition = scriptTypePosition;
				var enumerator = GetImmediateChildren(customDataProperty_Drawn);
				while (enumerator.MoveNext())
				{
					customDataPosition.y += EditorGUI.GetPropertyHeight(enumerator.Current) + EditorGUIUtility.standardVerticalSpacing;
					EditorGUI.PropertyField(customDataPosition, enumerator.Current);
				}

				CopyAllToRawData(customDataProperty_Drawn, customData);
				SetByteBuffer(customDataProperty_hidden, customData);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SetProperties(property);

			float height = EditorGUIUtility.singleLineHeight;

			if (customDataProperty_Drawn != null)
			{
				var enumerator = GetImmediateChildren(customDataProperty_Drawn);
				while (enumerator.MoveNext())
				{
					height += EditorGUI.GetPropertyHeight(enumerator.Current) + EditorGUIUtility.standardVerticalSpacing;
				}
			}

			return height;
		}

		private static byte[] GetByteBuffer(SerializedProperty fixedByteBuffer)
		{
			System.Diagnostics.Debug.Assert(fixedByteBuffer.isFixedBuffer);
			System.Diagnostics.Debug.Assert(fixedByteBuffer.type.GetTypeCode() == TypeCode.Byte);

			int length = fixedByteBuffer.fixedBufferSize;
			byte[] buffer = new byte[length];
			for (int i = 0; i < length; i++)
				buffer[i] = (byte)fixedByteBuffer.GetFixedBufferElementAtIndex(i).intValue;
			return buffer;
		}

		public static void SetByteBuffer(SerializedProperty fixedByteBuffer, byte[] buffer)
		{
			System.Diagnostics.Debug.Assert(fixedByteBuffer.isFixedBuffer);
			System.Diagnostics.Debug.Assert(fixedByteBuffer.type.GetTypeCode() == TypeCode.Byte);

			int length = fixedByteBuffer.fixedBufferSize;
			for (int i = 0; i < length; i++)
				fixedByteBuffer.GetFixedBufferElementAtIndex(i).intValue = buffer[i];
		}

		public static TypeCode GetPropertyType(SerializedProperty property)
		{
			string[] path = property.propertyPath.Split('.');
			System.Reflection.FieldInfo fi = null;
			Type type = property.serializedObject.targetObject.GetType();
			foreach (string name in path)
			{
				fi = type.GetField(name);
				type = fi?.FieldType;
			}
			return Type.GetTypeCode(type);
		}

		private static void SetThisFromRawData(SerializedProperty property, byte[] data, ref int index)
		{
			switch(GetPropertyType(property))
			{
				case TypeCode.Byte:
					property.intValue = data[index++];
					break;
				case TypeCode.SByte:
					property.intValue = (sbyte)data[index++];
					break;
				case TypeCode.Int16:
					property.intValue = BitConverter.ToInt16(data, index);
					index += 2;
					break;
				case TypeCode.UInt16:
					property.intValue = BitConverter.ToUInt16(data, index);
					index += 2;
					break;
				case TypeCode.Int32:
					property.intValue = BitConverter.ToInt32(data, index);
					index += 4;
					break;
				case TypeCode.UInt32:
					property.longValue = BitConverter.ToUInt32(data, index);
					index += 4;
					break;
				case TypeCode.Int64:
					property.longValue = BitConverter.ToInt64(data, index);
					index += 8;
					break;
				case TypeCode.UInt64:
					ulong val = BitConverter.ToUInt64(data, index);
					if (val > long.MaxValue)
					{
						UnityEngine.Debug.LogWarning("LiteNetworkedDataPropertyDrawer: UInt64 value is too large to fit in a long, clamping to long.MaxValue");
						property.longValue = long.MaxValue;
					}
					else property.longValue = (long)val;
					index += 8;
					break;
				case TypeCode.Single:
					property.floatValue = BitConverter.ToSingle(data, index);
					index += 4;
					break;
				case TypeCode.Double:
					property.doubleValue = BitConverter.ToDouble(data, index);
					index += 8;
					break;
				case TypeCode.Char:
					property.intValue = BitConverter.ToChar(data, index);
					index += 2;
					break;
			}
		}

		public static void SetAllFromRawData(SerializedProperty property, byte[] data)
		{
			int index = 0;
			try {
				IEnumerator<SerializedProperty> leafs = GetAllLeafProperties(property);
				while (leafs.MoveNext())
					SetThisFromRawData(leafs.Current, data, ref index);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("LiteNetworkedDataPropertyDrawer: Error setting property " + property.name + " from raw data at index " + index);
				UnityEngine.Debug.LogException(e);
			}
		}

		public static void CopyThisToRawData(SerializedProperty property, byte[] data, ref int index)
		{
			switch (GetPropertyType(property))
			{
				case TypeCode.Byte:
					data[index++] = (byte)property.intValue;
					break;
				case TypeCode.SByte:
					data[index++] = (byte)(sbyte)property.intValue;
					break;
				case TypeCode.Int16:
					BitConverter.GetBytes((short)property.intValue).CopyTo(data, index);
					index += 2;
					break;
				case TypeCode.UInt16:
					BitConverter.GetBytes((ushort)property.intValue).CopyTo(data, index);
					index += 2;
					break;
				case TypeCode.Int32:
					BitConverter.GetBytes(property.intValue).CopyTo(data, index);
					index += 4;
					break;
				case TypeCode.UInt32:
					BitConverter.GetBytes((uint)property.longValue).CopyTo(data, index);
					index += 4;
					break;
				case TypeCode.Int64:
					BitConverter.GetBytes(property.longValue).CopyTo(data, index);
					index += 8;
					break;
				case TypeCode.UInt64:
					BitConverter.GetBytes((ulong)property.longValue).CopyTo(data, index);
					index += 8;
					break;
				case TypeCode.Single:
					BitConverter.GetBytes(property.floatValue).CopyTo(data, index);
					index += 4;
					break;
				case TypeCode.Double:
					BitConverter.GetBytes(property.doubleValue).CopyTo(data, index);
					index += 8;
					break;
				case TypeCode.Char:
					BitConverter.GetBytes((char)property.intValue).CopyTo(data, index);
					index += 2;
					break;
			}
		}

		public static void CopyAllToRawData(SerializedProperty property, byte[] data)
		{
			int index = 0;
			try {
				IEnumerator<SerializedProperty> leafs = GetAllLeafProperties(property);
				while (leafs.MoveNext())
					CopyThisToRawData(leafs.Current, data, ref index);
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("LiteNetworkedDataPropertyDrawer: Error copying property " + property.name + " from raw data at index " + index);
				UnityEngine.Debug.LogException(e);
			}
		}

		private static IEnumerator<SerializedProperty> GetImmediateChildren(SerializedProperty property, bool copy = true)
		{
			if (copy) property = property.Copy();
			int depth = property.depth;
			bool enterChildren = true;
			while (property.NextVisible(enterChildren) && property.depth > depth)
			{
				enterChildren = false;
				yield return property;
			}
		}

		private static IEnumerator<SerializedProperty> GetAllLeafProperties(SerializedProperty property, bool copy = true)
		{
			if (copy) property = property.Copy();
			int depth = property.depth;
			while (property.Next(true))
			{
				if (property.depth <= depth) yield break;
				if (property.hasChildren) continue;
				yield return property;
			}
		}
	}
}