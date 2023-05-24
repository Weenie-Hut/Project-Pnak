using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class ShowWithDevice : MonoBehaviour
	{
		[System.Flags]
		public enum DeviceType
		{
			Mouse = 1 << 0,
			Keyboard = 1 << 1,
			Gamepad = 1 << 2,
			Touch = 1 << 3,
		}

		public DeviceType deviceType;
		public bool showIfDeviceNotPresent;
		[System.NonSerialized] private bool deviceExists;
		private bool Showing {
			get => showIfDeviceNotPresent ? !deviceExists : deviceExists;
		}

		private void Start()
		{
			deviceExists = ShouldShow(deviceType);
			gameObject.SetActive(Showing);

			InputSystem.onDeviceChange += OnDeviceChange;
		}

		private void OnDestroy()
		{
			InputSystem.onDeviceChange -= OnDeviceChange;
		}

		private void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (!Showing)
			{
				if (ShouldShowForDevice(device, deviceType))
				{
					deviceExists = true;
					gameObject.SetActive(Showing);
				}
			}
			else
			{
				if (!ShouldShow(deviceType))
				{
					deviceExists = false;
					gameObject.SetActive(Showing);
				}
			}
		}

		private static bool ShouldShow(DeviceType deviceType)
		{
			foreach (InputDevice inputDevice in InputSystem.devices)
			{
				if (ShouldShowForDevice(inputDevice, deviceType))
					return true;
			}

			return false;
		}

		private static bool ShouldShowForDevice(InputDevice inputDevice, DeviceType deviceType)
		{
			if (inputDevice is null)
				throw new ArgumentNullException(nameof(inputDevice));

			if (inputDevice.enabled == false)
				return false;

			switch (inputDevice)
			{
				case Mouse mouse:
					return (deviceType & DeviceType.Mouse) != 0;
				case Keyboard _:
					return (deviceType & DeviceType.Keyboard) != 0;
				case Gamepad _:
					return (deviceType & DeviceType.Gamepad) != 0;
				case Touchscreen _:
					return (deviceType & DeviceType.Touch) != 0;
				default:
					UnityEngine.Debug.LogWarning($"Unknown device type: {inputDevice.GetType()}. Consider adding it to {nameof(DeviceType)}.");
					return false;
			}
		}

	}
}