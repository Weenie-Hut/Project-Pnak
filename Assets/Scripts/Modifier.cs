using UnityEngine;
using Fusion;

namespace Pnak
{
	public enum ModifierTarget
	{
		Health = 0,
		Reload,
		Range,
		Damage,
		Lifetime,
	}

	public enum ApplyType
	{
		Add = 0,
		Multiply,
	}

	public enum ExpirationType
	{
		None = 0,
		Timeout,
	}

	public struct Modifier : INetworkStruct
	{
		public ModifierTarget type;
		public ApplyType applyType;
		public ExpirationType expirationType;
		public float value;

		public float ApplyValue(float baseValue)
		{
			switch (applyType)
			{
				case ApplyType.Add:
					return baseValue + value;
				case ApplyType.Multiply:
					return baseValue * value;
				default:
					return baseValue;
			}
		}

		public float RemoveValue(float baseValue)
		{
			switch (applyType)
			{
				case ApplyType.Add:
					return baseValue - value;
				case ApplyType.Multiply:
					return baseValue / value;
				default:
					return baseValue;
			}
		}
	}
}