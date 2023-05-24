using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

using InputContextAction = System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>;
using InputCallbackPair = System.Collections.Generic.KeyValuePair<string, System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>>;

namespace Pnak.Input
{
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

		public InputActionTriggered(string actionName, InputStateFilters stateFilter = InputStateFilters.None)
		{
			this.actionName = actionName;
			this.stateFilter = stateFilter;
		}

		private static InputContextAction NoneFilter(InputContextAction callback) =>
			callback;

		private static InputContextAction PerformedFilter(InputContextAction callback) =>
			context => { if (context.performed) callback(context); };

		private static InputContextAction StartedFilter(InputContextAction callback) =>
			context => { if (context.started) callback(context); };
		
		private static InputContextAction CanceledFilter(InputContextAction callback) =>
			context => { if (context.canceled) callback(context); };

		private static InputContextAction PreformedThisFrameFilter(InputContextAction callback) => context =>
		{
			if (context.performed && context.control is ButtonControl button && button.wasPressedThisFrame) callback(context);
		};

		private static InputContextAction ReleasedThisFrameFilter(InputContextAction callback) =>
			context => { if (context.control is ButtonControl button && button.wasReleasedThisFrame) callback(context); };

		private static InputContextAction FloatChangedFilter(InputContextAction callback)
		{
			float lastValue = float.NaN;
			return context =>
			{
				if (context.control is AxisControl axis && axis.ReadValue() != lastValue)
				{
					lastValue = axis.ReadValue();
					callback(context);
				}
			};
		}

		private static InputContextAction Vector2ChangedFilter(InputContextAction callback)
		{
			Vector2 lastValue = new Vector2(float.NaN, float.NaN);
			return context =>
			{
				if (context.control is Vector2Control vector && vector.ReadValue() != lastValue)
				{
					lastValue = vector.ReadValue();
					callback(context);
				}
			};
		}

		private static InputContextAction Vector3ChangedFilter(InputContextAction callback)
		{
			Vector3 lastValue = new Vector3(float.NaN, float.NaN, float.NaN);
			return context =>
			{
				if (context.control is Vector3Control vector && vector.ReadValue() != lastValue)
				{
					lastValue = vector.ReadValue();
					callback(context);
				}
			};
		}

		public InputContextAction FilteredCallback(InputContextAction callback)
		{
			switch (stateFilter)
			{
				case InputStateFilters.None: return NoneFilter(callback);
				case InputStateFilters.Performed: return PerformedFilter(callback);
				case InputStateFilters.Started: return StartedFilter(callback);
				case InputStateFilters.Canceled: return CanceledFilter(callback);
				case InputStateFilters.PreformedThisFrame: return PreformedThisFrameFilter(callback);
				case InputStateFilters.ReleasedThisFrame: return ReleasedThisFrameFilter(callback);
				case InputStateFilters.FloatChanged: return FloatChangedFilter(callback);
				case InputStateFilters.Vector2Changed: return Vector2ChangedFilter(callback);
				case InputStateFilters.Vector3Changed: return Vector3ChangedFilter(callback);
				default:
					Debug.LogError($"Unknown state filter {stateFilter}");
					return NoneFilter(callback);
			}
		}

		public InputCallbackPair CreateInputPair(System.Reflection.MethodInfo m, object obj)
		{
			InputContextAction callback = (InputContextAction)Delegate.CreateDelegate(typeof(InputContextAction), obj, m);
			return new InputCallbackPair(actionName, FilteredCallback(callback));
		}
	}
}