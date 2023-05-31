using System.Runtime.InteropServices;
using System;
using Fusion;

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
	}

	public interface IDamageReceiver
	{
		bool AddDamage(DamageAmount damage);
	}
}