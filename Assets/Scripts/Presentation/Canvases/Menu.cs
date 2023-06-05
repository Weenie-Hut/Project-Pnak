using System;
using System.Collections.Generic;
using UnityEngine;
using Pnak.Input;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class Menu : MonoBehaviour
	{
		[InputActionTriggered(ActionNames.ToggleMenu, InputStateFilters.PreformedThisFrame, InputMap.Gameplay)]
		private void ToggleMenu(InputAction.CallbackContext context)
		{
			RadialMenu.Instance.Show(GameManager.Instance.CharacterOptions, null);
		}

		private void Start()
		{
			InputCallbackSystem.SetupInputCallbacks(this);
			RadialMenu.Instance.Show(GameManager.Instance.CharacterOptions, null);
		}

		private void OnDestroy()
		{
			InputCallbackSystem.CleanupInputCallbacks(this);
		}

	}
}