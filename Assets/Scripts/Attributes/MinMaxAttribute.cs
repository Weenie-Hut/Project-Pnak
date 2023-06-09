using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class MinMaxAttribute : PropertyAttribute
	{
		public readonly object Min = 0;
		public readonly object Max = 1;

		public MinMaxAttribute(float min = float.MinValue, float max = float.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(int min = int.MinValue, int max = int.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(uint min = uint.MinValue, uint max = uint.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(byte min = byte.MinValue, byte max = byte.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(sbyte min = sbyte.MinValue, sbyte max = sbyte.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(short min = short.MinValue, short max = short.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(ushort min = ushort.MinValue, ushort max = ushort.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(long min = long.MinValue, long max = long.MaxValue)
		{
			Min = min;
			Max = max;
		}

		public MinMaxAttribute(ulong min = ulong.MinValue, ulong max = ulong.MaxValue)
		{
			Min = min;
			Max = max;
		}
	}
}

