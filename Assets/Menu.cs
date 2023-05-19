using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class Menu : MonoBehaviour
	{
		[SerializeField] private RectTransform CharacterOptions;
		[SerializeField] private GameObject CharacterOptionPrefab;

		private KeyValuePair<GameManager.Buttons, Action>[] _buttonActions;

		private void ToggleOff() => Toggle(false);
		private void Toggle(bool value)
		{
			if (value)
			{
				if (GameManager.Instance.PlayerInput.currentActionMap.name != "Menu")
					GameManager.Instance.PlayerInput.SwitchCurrentActionMap("Menu");
			}
			else if (GameManager.Instance.PlayerInput.currentActionMap.name != "Gameplay")
				GameManager.Instance.PlayerInput.SwitchCurrentActionMap("Gameplay");
			gameObject.SetActive(value);
		}

		private void Start()
		{
			_buttonActions = new KeyValuePair<GameManager.Buttons, Action>[] {
				new (GameManager.Buttons.MenuButton_1, ToggleOff),
				new (GameManager.Buttons.MenuButton_2, ToggleOff),
				new (GameManager.Buttons.MenuButton_3, ToggleOff),
				new (GameManager.Buttons.MenuButton_4, ToggleOff),
				new (GameManager.Buttons.ToggleMenu, () => Toggle(!gameObject.activeSelf)),
			};

			foreach (var buttonAction in _buttonActions)
				GameManager.Instance.AddButtonListener(buttonAction.Key, buttonAction.Value);

			// int index = 0;
			foreach (var character in GameManager.Instance.Characters)
			{
				var characterOption = Instantiate(CharacterOptionPrefab, CharacterOptions);
				var characterSelectUI = characterOption.GetComponent<CharacterSelectUI>();
				characterSelectUI.SetData(character);

				// var screenInput = characterOption.GetComponent<UnityEngine.InputSystem.OnScreen.OnScreenButton>();
				// screenInput.controlPath = $"<Keyboard>/{++index}";
			}
		}

		private void OnDestroy()
		{
			foreach (var buttonAction in _buttonActions)
				GameManager.Instance?.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

	}
}