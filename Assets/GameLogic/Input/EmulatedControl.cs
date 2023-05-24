
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Pnak.Input
{
	public class EmulatedControl
	{
		public EmulatedControl(string path)
		{
			Debug.Assert(m_Control == null, "InputControl already initialized");
			Debug.Assert(m_NextControlOnDevice == null, "Previous InputControl has not been properly uninitialized (m_NextControlOnDevice still set)");
			Debug.Assert(!m_InputEventPtr.valid, "Previous InputControl has not been properly uninitialized (m_InputEventPtr still set)");

			if (string.IsNullOrEmpty(path))
				return;

			// Determine what type of device to work with.
			var layoutName = InputControlPath.TryGetDeviceLayout(path);
			if (layoutName == null)
			{
				Debug.LogError(
					$"Cannot determine device layout to use based on control path '{path}'");
				return;
			}

			// Try to find existing on-screen device that matches.
			var internedLayoutName = new InternedString(layoutName);
			var deviceInfoIndex = -1;
			for (var i = 0; i < s_EmulateDevices.Count; ++i)
			{
				////FIXME: this does not take things such as different device usages into account
				if (s_EmulateDevices[i].device.layout == internedLayoutName)
				{
					deviceInfoIndex = i;
					break;
				}
			}

			// If we don't have a matching one, create a new one.
			InputDevice device;
			if (deviceInfoIndex == -1)
			{
				// Try to create device.
				try
				{
					device = InputSystem.AddDevice(layoutName);
				}
				catch (Exception exception)
				{
					Debug.LogError($"Could not create device with layout '{layoutName}'");
					Debug.LogException(exception);
					return;
				}
				InputSystem.AddDeviceUsage(device, "Emulated");

				// Create event buffer.
				var buffer = StateEvent.From(device, out var eventPtr, Allocator.Persistent);

				// Add to list.
				deviceInfoIndex = s_EmulateDevices.Count;
				s_EmulateDevices.Add(new EmulateDeviceInfo
				{
					eventPtr = eventPtr,
					buffer = buffer,
					device = device,
				});
			}
			else
			{
				device = s_EmulateDevices[deviceInfoIndex].device;
			}

			// Try to find control on device.
			m_Control = InputControlPath.TryFindControl(device, path);
			if (m_Control == null)
			{
				Debug.LogError(
					$"Cannot find control with path '{path}' on device of type '{layoutName}'");

				// Remove the device, if we just created one.
				if (s_EmulateDevices[deviceInfoIndex].firstControl == null)
				{
					s_EmulateDevices[deviceInfoIndex].Destroy();
					s_EmulateDevices.RemoveAt(deviceInfoIndex);
				}

				return;
			}
			m_InputEventPtr = s_EmulateDevices[deviceInfoIndex].eventPtr;

			// We have all we need. Permanently add us.
			s_EmulateDevices[deviceInfoIndex] =
				s_EmulateDevices[deviceInfoIndex].AddControl(this);
		}

		private InputControl m_Control;
		private EmulatedControl m_NextControlOnDevice;
		private InputEventPtr m_InputEventPtr;

		public void SendValueToControl<TValue>(TValue value)
			where TValue : struct
		{
			if (m_Control == null)
				return;

			if (!(m_Control is InputControl<TValue> control))
				throw new ArgumentException(
					$"The control path yields a control of type {m_Control.GetType().Name} which is not an InputControl with value type {typeof(TValue).Name}", nameof(value));

			////FIXME: this gives us a one-frame lag (use InputState.Change instead?)
			m_InputEventPtr.time = InputState.currentTime;
			control.WriteValueIntoEvent(value, m_InputEventPtr);
			InputSystem.QueueEvent(m_InputEventPtr);
		}

		public void SendDefaultValueToControl()
		{
			if (m_Control == null)
				return;

			////FIXME: this gives us a one-frame lag (use InputState.Change instead?)
			m_InputEventPtr.time = InputState.currentTime;
			m_Control.ResetToDefaultStateInEvent(m_InputEventPtr);
			InputSystem.QueueEvent(m_InputEventPtr);
		}

		~EmulatedControl()
		{
			Disable();
		}

		public void Disable()
		{
			if (m_Control == null)
				return;

			var device = m_Control.device;
			for (var i = 0; i < s_EmulateDevices.Count; ++i)
			{
				if (s_EmulateDevices[i].device != device)
					continue;

				var deviceInfo = s_EmulateDevices[i].RemoveControl(this);
				if (deviceInfo.firstControl == null)
				{
					// We're the last on-screen control on this device. Remove the device.
					s_EmulateDevices[i].Destroy();
					s_EmulateDevices.RemoveAt(i);
				}
				else
				{
					s_EmulateDevices[i] = deviceInfo;

					// We're keeping the device but we're disabling the on-screen representation
					// for one of its controls. If the control isn't in default state, reset it
					// to that now. This is what ensures that if, for example, OnScreenButton is
					// disabled after OnPointerDown, we reset its button control to zero even
					// though we will not see an OnPointerUp.
					if (!m_Control.CheckStateIsAtDefault())
						SendDefaultValueToControl();
				}

				m_Control = null;
				m_InputEventPtr = new InputEventPtr();
				Debug.Assert(m_NextControlOnDevice == null);

				break;
			}
		}

		private struct EmulateDeviceInfo
		{
			public InputEventPtr eventPtr;
			public NativeArray<byte> buffer;
			public InputDevice device;
			public EmulatedControl firstControl;

			public EmulateDeviceInfo AddControl(EmulatedControl control)
			{
				control.m_NextControlOnDevice = firstControl;
				firstControl = control;
				return this;
			}

			public EmulateDeviceInfo RemoveControl(EmulatedControl control)
			{
				if (firstControl == control)
					firstControl = control.m_NextControlOnDevice;
				else
				{
					for (EmulatedControl current = firstControl.m_NextControlOnDevice, previous = firstControl;
						 current != null; previous = current, current = current.m_NextControlOnDevice)
					{
						if (current != control)
							continue;

						previous.m_NextControlOnDevice = current.m_NextControlOnDevice;
						break;
					}
				}

				control.m_NextControlOnDevice = null;
				return this;
			}

			public void Destroy()
			{
				if (buffer.IsCreated)
					buffer.Dispose();
				if (device != null)
					InputSystem.RemoveDevice(device);
				device = null;
				buffer = new NativeArray<byte>();
			}
		}

		[NonSerialized] private static List<EmulateDeviceInfo> s_EmulateDevices;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			s_EmulateDevices = new List<EmulateDeviceInfo>();
			Application.quitting += () =>
			{
				while(s_EmulateDevices.Count > 0)
					s_EmulateDevices[0].firstControl.Disable();
			};
		}
	}
}