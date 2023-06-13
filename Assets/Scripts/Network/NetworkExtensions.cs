using System.Runtime.InteropServices;
using System;
using Fusion;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Pnak
{
	public static class NetworkExtensions
	{
		public static void DespawnSelf(this NetworkBehaviour networkBehaviour)
		{
			networkBehaviour.Runner.Despawn(networkBehaviour.Object);
		}

		public static byte[] ToBytes<T>(this T strct) where T : struct
		{
			int size = Marshal.SizeOf(strct);
			byte[] arr = new byte[size];

			IntPtr ptr = IntPtr.Zero;

			try {
				ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(strct, ptr, true);
				Marshal.Copy(ptr, arr, 0, size);
			}
			finally {
				if (ptr != IntPtr.Zero) {
					Marshal.FreeHGlobal(ptr);
				}
			}

			return arr;
		}

		public static T ToStruct<T>(this byte[] arr) where T : struct
		{
			T strct = new T();

			int size = Marshal.SizeOf(strct);

			IntPtr ptr = IntPtr.Zero;

			try {
				ptr = Marshal.AllocHGlobal(size);
				Marshal.Copy(arr, 0, ptr, size);
				strct = (T)Marshal.PtrToStructure(ptr, strct.GetType());
			}
			finally {
				if (ptr != IntPtr.Zero) {
					Marshal.FreeHGlobal(ptr);
				}
			}

			return strct;
		}

		public static string HexString(this byte[] bytes, string name = null)
		{
			string str = name != null ? name + ": " : "";
			for (int i = 0; i < bytes.Length; i++) {
				str += bytes[i].ToString("X2") + " ";
			}
			return str;
		}

		public static string Format(this IEnumerable array, string separator = ",", string prefix = "[", string suffix = "]")
		{
			if (array == null)
				return "<Format=NULL>";

			try {
				return prefix + string.Join(separator, array
					.Cast<object>()
					.Select(x => x == null ?
						"NULL" :
						(x is IEnumerable && !(x is string) ? (x as IEnumerable).Format() : x.ToString()))
					.ToArray()) + suffix;
			} catch { }

			return "<Format=invalid>";
		}

		public static string TrimAndPad(this string value, int length, bool right = false)
		{
			if (value.Length > length)
				value = value.Substring(0, length);
			else if (value.Length < length)
			{
				if (right)
					value = value.PadRight(length);
				else
					value = value.PadLeft(length);
			}

			return value;
		}

		public static float NaNTo0(this float value)
		{
			return float.IsNaN(value) ? 0 : value;
		}

		/// <summary>
		/// Returns a formatted string, where the indices, like '{0}' or `{1:P2}`, are set using the order of the id, like '{cost}' or '{amount:F1}'.
		/// </summary>
		/// <param name="format">The format string.</param>
		/// <param name="id">The id</param>
		/// <param name="args">The arguments to format.</param>
		/// <returns>Formatted string</returns>
		/// <example>
		/// "Cut speed by {speed:P0}, or {speed:F3}m/s".FormatById("speed", 0.513f) == "Cut speed by 51 %, or 0.513m/s"
		/// </example>
		public static string FormatById(this string format, string id, params object[] args)
		{
			if (format == null)
				return null;

			if (args == null)
				return format;

			if (id == null)
				return string.Format(format, args);

			int count = 0;
			return Regex.Replace(format, @"(?<!\{)\{(" + id + @")(\:([^\}]+))?\}(?!\})", m =>
			{
				object value;

				if (count >= args.Length)
					value = args.Last();
				else
					value = args[count];
				
				string arg;
				try {
					arg = m.Groups[3].Success ? string.Format("{0:" + m.Groups[3].Value + "}", value) : value.ToString();
				}
				catch { arg = "{FORMAT ERROR}"; }

				count++;
				return arg;
			});
		}

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
		
		public static bool InBounds<T>(this T[] array, int index)
		{
			return index >= 0 && index < array.Length;
		}

		public static bool InBounds<T>(this List<T> list, int index)
		{
			return index >= 0 && index < list.Count;
		}

		public static T SafeGet<T>(this List<T> list, int index)
		{
			if (index < 0 || index >= list.Count)
				return default(T);
			return list[index];
		}

		public static T ListLast<T>(this List<T> list)
		{
			return list[list.Count - 1];
		}
	}

	public interface IDamageReceiver
	{
		bool AddDamage(DamageAmount damage);
	}
}