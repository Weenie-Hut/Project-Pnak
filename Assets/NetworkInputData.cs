using Fusion;
using UnityEngine;

namespace Pnak
{
	public struct NetworkInputData : INetworkInput
	{
		public byte buttons;
		public Vector2 movement;

		public bool GetButton(int button)
		{
			return (buttons & (1 << button)) != 0;
		}

		public void SetButton(int button, bool value)
		{
			if (value)
			{
				buttons |= (byte)(1 << button);
			}
			else
			{
				buttons &= (byte)~(1 << button);
			}
		}
	}
}
