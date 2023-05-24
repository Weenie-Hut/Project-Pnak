using Fusion;
using UnityEngine;

namespace Pnak
{
	[System.Flags]
	public enum ControllerConfig
	{
		Gameplay = 1,
		Menu = 2,

		All = Gameplay | Menu
	}

	public struct NetworkInputData : INetworkInput
	{
		private byte rawData;
		private byte index;
		private float _mouseAngle;

		public Vector2 movement;

		public float AimAngle { get => _mouseAngle; set => _mouseAngle = value; }
		public Vector2 AimDirection
		{
			get => MathUtil.AngleToDirection(AimAngle);
			set => AimAngle = MathUtil.DirectionToAngle(value);
		}

		public byte Index { get => index; set => index = value; }

		private bool GetBoolean(byte button)
		{
			return (rawData & button) != 0;
		}

		private void SetButton(byte button, bool value)
		{
			if (value)
				rawData |= button;
			else
				rawData &= (byte)~(button);
		}

		public byte GetData(byte mask, byte shift)
		{
			return (byte)((rawData & mask) >> shift);
		}

		public void SetData(byte mask, byte shift, byte value)
		{
			rawData = (byte)((rawData & ~mask) | ((value << shift) & mask));
		}
		
		public const byte Button1 = 0b0000_0001;
		public bool Button1Pressed
		{
			get => GetBoolean(Button1);
			set => SetButton(Button1, value);
		}

		public const byte Button2 = 0b0000_0010;
		public bool Button2Pressed
		{
			get => GetBoolean(Button2);
			set => SetButton(Button2, value);
		}

		public const byte Button3 = 0b0000_0100;
		public bool Button3Pressed
		{
			get => GetBoolean(Button3);
			set => SetButton(Button3, value);
		}

		public const byte Button4 = 0b0000_1000;
		public bool Button4Pressed
		{
			get => GetBoolean(Button4);
			set => SetButton(Button4, value);
		}

		public const byte Button5 = 0b0001_0000;
		public bool Button5Pressed
		{
			get => GetBoolean(Button5);
			set => SetButton(Button5, value);
		}

		public const byte Button6 = 0b0010_0000;
		public bool Button6Pressed
		{
			get => GetBoolean(Button6);
			set => SetButton(Button6, value);
		}

		public const byte ControllerConfigMask = 0b1100_0000;
		public const byte ControllerConfigShift = 6;
		public ControllerConfig ControllerConfig
		{
			get => (ControllerConfig)GetData(ControllerConfigMask, ControllerConfigShift);
			set => SetData(ControllerConfigMask, ControllerConfigShift, (byte)value);
		}

		public void ClearButtons()
		{
			rawData = (byte)(rawData >> ControllerConfigShift << ControllerConfigShift);
		}


		public void LogValues()
		{
			Debug.Log("{ " +
				"Button1: " + Button1Pressed + ", " +
				"Button2: " + Button2Pressed + ", " +
				"Button3: " + Button3Pressed + ", " +
				"Button4: " + Button4Pressed + ", " +
				"Button5: " + Button5Pressed + ", " +
				"Button6: " + Button6Pressed + ", " +
				"ControllerConfig: " + ControllerConfig +
				" }");
		}
	}
}
