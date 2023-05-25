using System;
using UnityEngine;

namespace Pnak
{
	public static class MathUtil
	{
		public static Vector2 AngleToDirection(float angle)
		{
			return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
		}

		

		public static float DirectionToAngle(Vector2 value)
		{
			if (value.x == 0f)
			{
				if (value.y > 0f)
					return 90f;
				else if (value.y < 0f)
					return 270f;
				else
					return 0f;
			}
			return Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
		}

		public static byte GetMaskShift(byte data, byte mask, byte shift)
		{
			return (byte)((data & mask) >> shift);
		}

		public static byte SetMaskShift(ref byte data, byte mask, byte shift, byte value)
		{
			return data = (byte)((data & ~mask) | ((value << shift) & mask));
		}

		public struct Half
		{
			public const ushort MaxValue = 0x7bff;
			public const ushort MinValue = 0xfbff;
			public const ushort NaN = 0x7e00;
			public const ushort NegativeInfinity = 0xfc00;
			public const ushort PositiveInfinity = 0x7c00;
			public const float EpsilonF = 0.00097656f;
			public const short Epsilon = 0x8;

			private ushort value;
			public ushort data => value;

			public Half(ushort data)
			{
				this.value = data;
			}

			public Half(float value)
			{
				this.value = FloatToData(value);
			}

			/// <summary>
			/// Returns the float as a half. This should only be used for compression and not math.
			/// Always preform math on floats.
			/// </summary>
			public static explicit operator Half(float value)
			{
				return new Half(value);
			}

			public static implicit operator float(Half value)
			{
				return DataToFloat(value.value);
			}

			public static ushort FloatToData(float value)
			{
				int fbits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
				int sign = (fbits >> 16) & 0x8000;
				int val = ((fbits & 0x7fffffff) >> 13) - (0x7f << 10);
				if (val <= 0)
				{
					if (val < -10)
						return (ushort)sign;
					val = 0;
				}
				else if (val >= 0x1f)
				{
					if (val >= 0x40)
						return (ushort)(sign | 0x7c00);
					val = 0x1f;
				}
				else
					val -= 0x70;
				return (ushort)(sign | (val << 10) | ((fbits >> 13) & 0x3ff));
			}

			public static float DataToFloat(ushort value)
			{
				int sign = (value & 0x8000) << 16;
				int val = (value & 0x7fff) << 13;
				if (val == 0x7f800000)
					return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7f800000), 0);
				if (val == 0)
					return BitConverter.ToSingle(BitConverter.GetBytes(sign), 0);
				if (val > 0x477fe000)
					return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7f800000), 0);
				if (val < 0x38800000)
				{
					int mant = (0x70 << 23) | (val >> 1);
					if ((val & 1) == 1 && (val & 0x3f) > 0)
						mant++;
					return BitConverter.ToSingle(BitConverter.GetBytes(sign | mant), 0);
				}
				int mant1 = (val >> 13) - 0x3c00;
				int mant2 = (val & 0x1fff) << 13;
				return BitConverter.ToSingle(BitConverter.GetBytes(sign | mant1 << 13 | mant2), 0);
			}
		}
	}

}