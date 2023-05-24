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
		[SerializeField] private DeviceSpriteLib ControllerButtons;

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
				GameInput.Instance.SetInputMap(InputMap.Menu);
			else GameInput.Instance.SetInputMap(InputMap.Gameplay);
			gameObject.SetActive(value);
		}

		private void Start()
		{
			InputEmulation.EmulateDelegate[] menuButtons = new InputEmulation.EmulateDelegate[]
			{
				InputEmulation.CreateEmulateButtonDelegate(ActionNames.Menu_Button1),
				InputEmulation.CreateEmulateButtonDelegate(ActionNames.Menu_Button2),
				InputEmulation.CreateEmulateButtonDelegate(ActionNames.Menu_Button3),
				InputEmulation.CreateEmulateButtonDelegate(ActionNames.Menu_Button4),
			};

			Sprite[] sprites = new Sprite[]
			{
				ControllerButtons.PrimaryButton,
				ControllerButtons.SecondaryButton,
				ControllerButtons.TertiaryButton,
				ControllerButtons.QuaternaryButton,
			};

			for (int i = 0; i < GameManager.Instance.Characters.Length; i++)
			{
				CharacterData character = GameManager.Instance.Characters[i];
				GameObject characterOption = Instantiate(CharacterOptionPrefab, CharacterOptions);
				CharacterSelectUI characterSelectUI = characterOption.GetComponent<CharacterSelectUI>();
				characterSelectUI.SetData(character);

				characterSelectUI.buttonIcon.sprite = sprites[i];

				var button = characterOption.GetComponent<UnityEngine.UI.Button>();
				button.onClick.AddListener(menuButtons[i].Invoke);
			}

			InputCallbackSystem.SetupInputCallbacks(this);
		}

		private void OnDestroy()
		{
			InputCallbackSystem.CleanupInputCallbacks(this);
		}

	}
}