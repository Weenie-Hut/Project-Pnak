using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Pnak.Input
{
	public class EmulatedAction
	{
		private EmulatedControl emulatedControl;

		public EmulatedAction(string actionNameOrId)
		{
			var action = InputEmulation.ActionAsset.FindAction(actionNameOrId);

			if (action == null)
			{
				Debug.LogError($"Could not find action with name or id '{actionNameOrId}'");
				return;
			}

			if (action.bindings.Count == 0)
			{
				Debug.LogError($"Action '{actionNameOrId}' has no bindings");
				return;
			}

			string path = GetPathFromBindings(action.bindings);

			emulatedControl = new EmulatedControl(path);
		}

		public void SendValueToControl<TValue>(TValue value)
			where TValue : struct => emulatedControl.SendValueToControl<TValue>(value);

		public void SendDefaultValueToControl() => emulatedControl.SendDefaultValueToControl();



		private static string GetPathFromBindings(ReadOnlyArray<InputBinding> bindings)
		{
			// First, try to find a binding that is not part of a composite.
			for (int i = 0; i < bindings.Count; i++)
			{
				var binding = bindings[i];

				if (binding.isComposite || binding.isPartOfComposite)
					continue;

				return binding.path;
			}

			return bindings[0].path;
		}
	}
}