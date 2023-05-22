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
				if (GameManager.Instance.InputData.ControllerConfig != ControllerConfig.Menu)
					GameManager.Instance.SetControllerConfig(ControllerConfig.Menu);

			}
			else if (GameManager.Instance.InputData.ControllerConfig != ControllerConfig.Gameplay)
				GameManager.Instance.SetControllerConfig(ControllerConfig.Gameplay);
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

			UnityEngine.Events.UnityAction[] menuButtons = new UnityEngine.Events.UnityAction[]
			{
				() => {
					GameManager.Instance.InvokeButtonListener(GameManager.Buttons.MenuButton_1);
					GameManager.Instance.InputData.Button3Pressed = true;
				},
				() => {
					GameManager.Instance.InvokeButtonListener(GameManager.Buttons.MenuButton_2);
					GameManager.Instance.InputData.Button4Pressed = true;
				},
				() => {
					GameManager.Instance.InvokeButtonListener(GameManager.Buttons.MenuButton_3);
					GameManager.Instance.InputData.Button5Pressed = true;
				},
				() => {
					GameManager.Instance.InvokeButtonListener(GameManager.Buttons.MenuButton_4);
					GameManager.Instance.InputData.Button6Pressed = true;
				}
			};

			for (int i = 0; i < GameManager.Instance.Characters.Length; i++)
			{
				CharacterData character = GameManager.Instance.Characters[i];
				var characterOption = Instantiate(CharacterOptionPrefab, CharacterOptions);
				var characterSelectUI = characterOption.GetComponent<CharacterSelectUI>();
				characterSelectUI.SetData(character);

				var button = characterOption.GetComponent<UnityEngine.UI.Button>();
				GameManager.Buttons index = (GameManager.Buttons)(GameManager.Buttons.MenuButton_1 + i);
				button.onClick.AddListener(menuButtons[i]);
			}
		}

		private void OnDestroy()
		{
			foreach (var buttonAction in _buttonActions)
				GameManager.Instance?.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

	}
}