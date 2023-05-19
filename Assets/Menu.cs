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

			for (int i = 0; i < GameManager.Instance.Characters.Length; i++)
			{
				CharacterData character = GameManager.Instance.Characters[i];
				var characterOption = Instantiate(CharacterOptionPrefab, CharacterOptions);
				var characterSelectUI = characterOption.GetComponent<CharacterSelectUI>();
				characterSelectUI.SetData(character);

				var button = characterOption.GetComponent<UnityEngine.UI.Button>();
				GameManager.Buttons index = (GameManager.Buttons)(GameManager.Buttons.MenuButton_1 + i);
				button.onClick.AddListener(() => GameManager.Instance.InvokeButtonListener(index));
			}
		}

		private void OnDestroy()
		{
			foreach (var buttonAction in _buttonActions)
				GameManager.Instance?.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

	}
}