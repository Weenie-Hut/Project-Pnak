using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Pnak.Input
{
	public static class InputEmulation
	{
		public static InputActionAsset ActionAsset { get; private set; }
		public static void SetActionAsset(InputActionAsset actionAsset) => ActionAsset = actionAsset;

		public static Dictionary<string, EmulatedAction> Actions { get; private set; } = new Dictionary<string, EmulatedAction>();

		public static EmulatedAction GetEmulatedAction(string actionNameOrId, bool create = true)
		{
			if (!Actions.TryGetValue(actionNameOrId, out EmulatedAction action))
			{
				if (!create) return null;

				action = new EmulatedAction(actionNameOrId);
				Actions.Add(actionNameOrId, action);
			}

			return action;
		}

		public static void EmulateAction<T>(string actionName, T value) where T : struct
		{
			EmulatedAction action = GetEmulatedAction(actionName);
			action.SendValueToControl(value);
		}

		public static void ClearEmulation(string actionName)
		{
			EmulatedAction action = GetEmulatedAction(actionName, false);
			action?.SendDefaultValueToControl();
		}

		public static void EmulateButton(string actionName)
		{
			EmulatedAction action = GetEmulatedAction(actionName);
			action.SendValueToControl(1f);
			action.SendValueToControl(0f);
		}

		public delegate void EmulateDelegate();

		public static EmulateDelegate CreateEmulateDelegate<T>(string actionName, T value) where T : struct
		{
			EmulatedAction action = GetEmulatedAction(actionName);
			return () => action.SendValueToControl(value);
		}

		public static EmulateDelegate CreateEmulateButtonDelegate(string actionName)
		{
			EmulatedAction action = GetEmulatedAction(actionName);
			return () =>
			{
				action.SendValueToControl(1f);
				action.SendValueToControl(0f);
			};
		}
	}
}