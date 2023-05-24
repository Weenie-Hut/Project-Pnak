using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public struct NetworkInputData : INetworkInput
	{
#region Data
		private Vector2 movement;
		private float _mouseAngle;
		private byte buttonDownData;
		private byte buttonPressed;
		private byte actionMap_and_extraData;
#endregion

		public Vector2 Movement { get => movement; set => movement = value; }

		public float AimAngle { get => _mouseAngle; set => _mouseAngle = value; }
		public Vector2 AimDirection
		{
			get => MathUtil.AngleToDirection(AimAngle);
			set => AimAngle = MathUtil.DirectionToAngle(value);
		}

		public bool GetButtonDown(byte button)
		{
			byte mask = (byte)(1 << button);
			return (buttonDownData & mask) != 0;
		}

		public bool GetButtonUp(byte button)
		{
			byte mask = (byte)(1 << button);
			return (buttonDownData & mask) == 0;
		}

		public bool GetButtonPressed(byte button)
		{
			byte mask = (byte)(1 << button);
			return (buttonPressed & mask) != 0;
		}

		public void SetButtonDown(byte button, bool value = true)
		{
			byte mask = (byte)(1 << button);
			if (((buttonDownData & mask) != 0) == value)
				return;

			if (value)
			{
				buttonDownData |= mask;
				buttonPressed |= mask;
			}
			else
			{
				buttonDownData &= (byte)~(mask);
			}
		}

#region InputMap_and_ExtraData
		public const byte InputMapMask = 0b1100_0000;
		public const byte InputMapShift = 6;
		public Input.InputMap CurrentInputMap
		{
			get => (Input.InputMap)MathUtil.GetMaskShift(actionMap_and_extraData, InputMapMask, InputMapShift);
			set => MathUtil.SetMaskShift(ref actionMap_and_extraData, InputMapMask, InputMapShift, (byte)value);
		}

		public const byte ExtraDataMask = 0b0011_1111;
		public const byte ExtraDataShift = 0;
		/// <summary>
		/// Extra data that can be passed for input, cleared every update. Max value is 63.
		/// </summary>
		public byte ExtraData
		{
			get => MathUtil.GetMaskShift(actionMap_and_extraData, ExtraDataMask, ExtraDataShift);
			set => MathUtil.SetMaskShift(ref actionMap_and_extraData, ExtraDataMask, ExtraDataShift, value);
		}
#endregion

		public void ClearState()
		{
			buttonPressed = 0;
			ExtraData = 0;
		}

		internal string Format()
		{
			string s = "NetworkInputData: {\n";
			s += $"  movement: {movement}\n";
			s += $"  mouseAngle: {_mouseAngle}\n";
			s += $"  buttonDownData: {buttonDownData}\n";
			s += $"  buttonChangedData: {buttonPressed}\n";
			s += $"  actionMap_and_extraData: {actionMap_and_extraData}\n";
			s += "}";

			return s;
		}
	}
}
