using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pnak.Input
{
	public delegate void InputContextAction(UnityEngine.InputSystem.InputAction.CallbackContext context);

	public enum InputStateFilters {
		None, Performed, Started, Canceled,
		PreformedThisFrame, ReleasedThisFrame,
		FloatChanged, Vector2Changed, Vector3Changed,
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class InputActionTriggered : Attribute
	{
		public string actionName;
		public InputStateFilters stateFilter;

		// TODO: Add filter support for controller config?
		public InputActionTriggered(string actionName, InputStateFilters stateFilter = InputStateFilters.None)
		{
			this.actionName = actionName;
			this.stateFilter = stateFilter;
		}

		private static bool PerformedFilter(InputAction.CallbackContext context) => context.performed;
		private static bool StartedFilter(InputAction.CallbackContext context) => context.started;
		private static bool CanceledFilter(InputAction.CallbackContext context) => context.canceled;
		private static bool PreformedThisFrameFilter(InputAction.CallbackContext context)
		{
			if (context.control is ButtonControl button)
				return context.performed && button.wasPressedThisFrame;
			Debug.LogWarning($"InputActionTriggered attribute with state filter {InputStateFilters.PreformedThisFrame} was used on an action that is not a button.");
			return false;
		}
		private static bool ReleasedThisFrameFilter(InputAction.CallbackContext context)
		{
			if (context.control is ButtonControl button)
				return context.canceled && button.wasReleasedThisFrame;
			Debug.LogWarning($"InputActionTriggered attribute with state filter {InputStateFilters.ReleasedThisFrame} was used on an action that is not a button.");
			return false;
		}
		private static Predicate<InputAction.CallbackContext> CreateFloatChangedFilter()
		{
			float lastValue = float.NaN;
			return context =>
			{
				if (context.control is AxisControl axis)
				{
					float value = axis.ReadValue();
					if (value != lastValue)
					{
						lastValue = value;
						return true;
					}
					return false;
				}
				Debug.LogWarning($"InputActionTriggered attribute with state filter {InputStateFilters.FloatChanged} was used on an action that is not an axis.");
				return false;
			};
		}
		private static Predicate<InputAction.CallbackContext> CreateVector2ChangedFilter()
		{
			Vector2 lastValue = Vector2.zero;
			return context =>
			{
				if (context.control is Vector2Control vector)
				{
					Vector2 value = vector.ReadValue();
					if (value != lastValue)
					{
						lastValue = value;
						return true;
					}
					return false;
				}
				Debug.LogWarning($"InputActionTriggered attribute with state filter {InputStateFilters.Vector2Changed} was used on an action that is not a vector2.");
				return false;
			};
		}
		private static Predicate<InputAction.CallbackContext> CreateVector3ChangedFilter()
		{
			Vector3 lastValue = Vector3.zero;
			return context =>
			{
				if (context.control is Vector3Control vector)
				{
					Vector3 value = vector.ReadValue();
					if (value != lastValue)
					{
						lastValue = value;
						return true;
					}
					return false;
				}
				Debug.LogWarning($"InputActionTriggered attribute with state filter {InputStateFilters.Vector3Changed} was used on an action that is not a vector3.");
				return false;
			};
		}

		public Predicate<InputAction.CallbackContext> GetFilteredCallback()
		{
			switch (stateFilter)
			{
				case InputStateFilters.None: return null;
				case InputStateFilters.Performed: return PerformedFilter;
				case InputStateFilters.Started: return StartedFilter;
				case InputStateFilters.Canceled: return CanceledFilter;
				case InputStateFilters.PreformedThisFrame: return PreformedThisFrameFilter;
				case InputStateFilters.ReleasedThisFrame: return ReleasedThisFrameFilter;
				case InputStateFilters.FloatChanged: return CreateFloatChangedFilter();
				case InputStateFilters.Vector2Changed: return CreateVector2ChangedFilter();
				case InputStateFilters.Vector3Changed: return CreateVector3ChangedFilter();
				default:
					UnityEngine.Debug.LogError($"Unknown InputStateFilter: {stateFilter}");
					return null;
			}
		}
	}
}