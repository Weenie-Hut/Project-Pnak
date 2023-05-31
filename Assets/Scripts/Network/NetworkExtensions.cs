using System.Runtime.InteropServices;
using System;
using Fusion;
using System.Collections;
using System.Linq;

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
	}

	public interface IDamageReceiver
	{
		bool AddDamage(DamageAmount damage);
	}
}