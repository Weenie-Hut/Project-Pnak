/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using System;

namespace Pnak
{
	public struct MutliType
	{
		public string _string;
		public long? _long;
		public float? _float;
		public bool? _bool;

		public bool HasAny => !string.IsNullOrEmpty(_string) || _long.HasValue || _float.HasValue || _bool.HasValue;

		public string String {
			get {
				if (!string.IsNullOrEmpty(_string))
					return _string;
				
				if (_long.HasValue)
					return _long.Value.ToString();

				if (_float.HasValue)
					return _float.Value.ToString();
				
				return _bool == true ? "true" : "false";
			}
		}

		public long Long {
			get {
				if (_long.HasValue)
					return _long.Value;
				
				if (!string.IsNullOrEmpty(_string))
				{
					if (long.TryParse(_string, out long result))
						return result;
					else return long.MinValue;
				}

				if (_float.HasValue)
					return (long)_float.Value;
				
				return _bool == true ? 1 : 0;
			}
		}

		public float Float {
			get {
				if (_float.HasValue)
					return _float.Value;
				
				if (!string.IsNullOrEmpty(_string))
				{
					if (float.TryParse(_string, out float result))
						return result;
					else return float.NaN;
				}

				if (_long.HasValue)
					return _long.Value;
				
				return _bool == true ? 1f : 0f;
			}
		}

		public bool Bool {
			get {
				if (_bool.HasValue)
					return _bool.Value;
				
				if (!string.IsNullOrEmpty(_string))
				{
					if (bool.TryParse(_string, out bool result))
						return result;
				}

				if (_long.HasValue)
					return _long.Value != 0;
				
				if (_float.HasValue)
					return _float.Value != 0f;
				
				return false;
			}
		}

		public bool TryGetType(System.Type type, out object result)
		{
			if (type == typeof(string))
			{
				result = String;
				return true;
			}
			else if (type.IsWholeNumeric())
			{
				result = Long;
				return true;
			}
			else if (type == typeof(float))
			{
				result = Float;
				return true;
			}
			else if (type == typeof(bool))
			{
				result = Bool;
				return true;
			}
			else
			{
				result = null;
				return false;
			}
		}

		public static implicit operator MutliType(string value) => new MutliType { _string = value };
		public static implicit operator MutliType(long value) => new MutliType { _long = value };
		public static implicit operator MutliType(float value) => new MutliType { _float = value };
		public static implicit operator MutliType(bool value) => new MutliType { _bool = value };

		public static MutliType Create(object value)
		{
			try {
				if (value is string)
					return new MutliType { _string = (string)value };
				else if (value.GetType().IsWholeNumeric())
					return new MutliType { _long = value.AsWholeNumeric() };
				else if (value is float)
					return new MutliType { _float = (float)value };
				else if (value is bool)
					return new MutliType { _bool = (bool)value };
				else
					return new MutliType();
			}
			catch (System.Exception) {
				UnityEngine.Debug.Log($"Failed to create MutliType from {value} ({value.GetType()})");
				return new MutliType();
			}
		}

		public override string ToString() => String;
	}

	public static class NumericExtensions
	{
		public static bool IsWholeNumeric(this Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return true;
				default:
					return type.IsEnum;
			}
		}

		public static long AsWholeNumeric(this object obj)
		{
			switch (Type.GetTypeCode(obj.GetType()))
			{
				case TypeCode.Byte:
					return (byte)obj;
				case TypeCode.SByte:
					return (sbyte)obj;
				case TypeCode.UInt16:
					return (ushort)obj;
				case TypeCode.UInt32:
					return (uint)obj;
				case TypeCode.UInt64:
					return (long)(ulong)obj;
				case TypeCode.Int16:
					return (short)obj;
				case TypeCode.Int32:
					return (int)obj;
				case TypeCode.Int64:
					return (long)obj;
				default:
					if (obj.GetType().IsEnum)
						return (long)obj;
					else
						throw new ArgumentException("Not a whole numeric type");
			}
		}
	}
}