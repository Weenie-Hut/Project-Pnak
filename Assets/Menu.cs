using System;
using System.Collections.Generic;
using UnityEngine;
using Pnak.Input;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class Menu : MonoBehaviour
	{
		[SerializeField] private RectTransform CharacterOptions;
		[SerializeField] private GameObject CharacterOptionPrefab;

		private KeyValuePair<GameManager.ButtonAction, Action>[] _buttonActions;

		[InputActionTriggered(ActionNames.Menu_Button1, InputStateFilters.PreformedThisFrame)]
		[InputActionTriggered(ActionNames.Menu_Button2, InputStateFilters.PreformedThisFrame)]
		[InputActionTriggered(ActionNames.Menu_Button3, InputStateFilters.PreformedThisFrame)]
		[InputActionTriggered(ActionNames.Menu_Button4, InputStateFilters.PreformedThisFrame)]
		private void ToggleOff(InputAction.CallbackContext context) => Toggle(false);

		[InputActionTriggered(ActionNames.ToggleMenu, InputStateFilters.PreformedThisFrame)]
		private void ToggleActive(InputAction.CallbackContext context) => Toggle(!gameObject.activeSelf);

		private void Toggle(bool value)
		{
			if (value)
			{
				GameManager.Instance.SetControllerConfig(ControllerConfig.Menu);
			}
			else GameManager.Instance.SetControllerConfig(ControllerConfig.Gameplay);
			gameObject.SetActive(value);
		}

		private void Start()
		{
			UnityEngine.Events.UnityAction[] menuButtons = new UnityEngine.Events.UnityAction[]
			{
				() => InputEmulation.EmulateButton(ActionNames.Menu_Button1),
				() => InputEmulation.EmulateButton(ActionNames.Menu_Button2),
				() => InputEmulation.EmulateButton(ActionNames.Menu_Button3),
				() => InputEmulation.EmulateButton(ActionNames.Menu_Button4),
			};

			for (int i = 0; i < GameManager.Instance.Characters.Length; i++)
			{
				CharacterData character = GameManager.Instance.Characters[i];
				var characterOption = Instantiate(CharacterOptionPrefab, CharacterOptions);
				var characterSelectUI = characterOption.GetComponent<CharacterSelectUI>();
				characterSelectUI.SetData(character);

				var button = characterOption.GetComponent<UnityEngine.UI.Button>();
				GameManager.ButtonAction index = (GameManager.ButtonAction)(GameManager.ButtonAction.MenuButton_1 + i);
				button.onClick.AddListener(menuButtons[i]);
			}

			InputCallbackSystem.RegisterInputCallbacks(this);
		}

		private void OnDestroy()
		{
			InputCallbackSystem.UnregisterInputCallbacks(this);
		}

	}
}