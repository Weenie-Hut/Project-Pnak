using System;
using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "DeviceSpriteLib", menuName = "Pnak/Device Sprite Library")]
	public class DeviceSpriteLib : ScriptableObject
	{
		public Sprite PrimaryButton;
		public Sprite SecondaryButton;
		public Sprite TertiaryButton;
		public Sprite QuaternaryButton;
	}
}