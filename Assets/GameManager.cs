using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Pnak
{
	public class GameManager : SingletonMono<GameManager>
	{
		public enum Buttons
		{
			ToggleMenu,
			MenuButton_1,
			MenuButton_2,
			MenuButton_3,
			MenuButton_4
		}

		[Tooltip("The character data to use for each character type. Temporary until we have character prefabs.")]
		public CharacterData[] Characters;

		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public class InputActionTriggered : Attribute
		{
			public string actionName;

			public InputActionTriggered(string actionName)
			{
				this.actionName = actionName;
			}
		}

		private NetworkInputData _inputData;

		public Camera MainCamera { get; private set; }
		public PlayerInput PlayerInput { get; private set;}
		public EventSystem EventSystem { get; private set; }
		public SceneLoader SceneLoader { get; private set; }
		public InputSystemUIInputModule InputSystemUIInputModule { get; private set; }
		public Vector2 GetMovement() => _inputData.movement;
		public bool GetButton(int button) => _inputData.GetButton(button);

		protected override void Awake()
		{
			base.Awake();

			_inputData = new NetworkInputData();

			MainCamera = Camera.main;
			DontDestroyOnLoad(MainCamera.gameObject);
			SceneLoader = FindObjectOfType<SceneLoader>();
			PlayerInput = GetComponent<PlayerInput>();
			EventSystem = GetComponent<EventSystem>();
			InputSystemUIInputModule = GetComponent<InputSystemUIInputModule>();

			PopulateActionDictionary();

			PlayerInput.onActionTriggered += context => OnActionTriggered(context);
			PlayerInput.uiInputModule = InputSystemUIInputModule;

			PlayerInput.SwitchCurrentActionMap("Menu");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (PlayerInput != null)
				PlayerInput.onActionTriggered -= context => OnActionTriggered(context);
		}

		public NetworkInputData GetNetworkInput()
		{
			var result = _inputData;

			_inputData.buttons = 0;

			return result;
		}

		private Dictionary<string, Action<InputAction.CallbackContext>> _actionDictionary = new Dictionary<string, Action<InputAction.CallbackContext>>();

		private void PopulateActionDictionary()
		{
			// Gets all "InputActionTriggered" methods in this class
			var entries = 
				GetType()
				.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SelectMany(m =>
					m.GetCustomAttributes(typeof(InputActionTriggered), false)
					.Select(a =>
						new KeyValuePair<string, Action<InputAction.CallbackContext>>(
							((InputActionTriggered)a).actionName,
							(Action<InputAction.CallbackContext>)Delegate.CreateDelegate(typeof(Action<InputAction.CallbackContext>), this, m)
						)
					)
				);

			foreach (var entry in entries)
			{
				if (!_actionDictionary.ContainsKey(entry.Key))
					_actionDictionary.Add(entry.Key, entry.Value);
				else
					_actionDictionary[entry.Key] += entry.Value;
			}
		}

		[InputActionTriggered("Move")]
		private void OnMoveTriggered(InputAction.CallbackContext context)
		{
			_inputData.movement = context.ReadValue<Vector2>();
		}

		[InputActionTriggered("Shoot")]
		private void OnShootTriggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			_inputData.SetButton(0, true);
		}

		[InputActionTriggered("MenuButton_1")]
		private void OnMenuButton1Triggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			UnityEngine.Debug.Log("MenuButton_1 Pressed");
			InvokeButtonListener(Buttons.MenuButton_1);
		}

		[InputActionTriggered("MenuButton_2")]
		private void OnMenuButton2Triggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			UnityEngine.Debug.Log("MenuButton_2 Pressed");
			InvokeButtonListener(Buttons.MenuButton_2);
		}

		[InputActionTriggered("MenuButton_3")]
		private void OnMenuButton3Triggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			UnityEngine.Debug.Log("MenuButton_3 Pressed");
			InvokeButtonListener(Buttons.MenuButton_3);
		}

		[InputActionTriggered("MenuButton_4")]
		private void OnMenuButton4Triggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			UnityEngine.Debug.Log("MenuButton_4 Pressed");
			InvokeButtonListener(Buttons.MenuButton_4);
		}

		[InputActionTriggered("ToggleMenu")]
		private void OnToggleMenuTriggered(InputAction.CallbackContext context)
		{
			if (!context.ReadValueAsButton()) return;
			UnityEngine.Debug.Log("ToggleMenu Pressed");

			_inputData.movement = Vector2.zero; // Reset movement

			InvokeButtonListener(Buttons.ToggleMenu);
		}

		private void OnActionTriggered(InputAction.CallbackContext context)
		{
			if (_actionDictionary.TryGetValue(context.action.name, out Action<InputAction.CallbackContext> action))
			{
				action.Invoke(context);
			}
		}

		public Dictionary<Buttons, Action> ButtonActions = new Dictionary<Buttons, Action>();

		public void AddButtonListener(Buttons button, Action action)
		{
			if (ButtonActions.ContainsKey(button))
			{
				ButtonActions[button] += action;
			}
			else
			{
				ButtonActions.Add(button, action);
			}
		}

		public void RemoveButtonListener(Buttons button, Action action)
		{
			if (ButtonActions.ContainsKey(button))
			{
				ButtonActions[button] -= action;

				if (ButtonActions[button] == null)
				{
					ButtonActions.Remove(button);
				}
			}
		}

		private void InvokeButtonListener(Buttons button)
		{
			if (ButtonActions.ContainsKey(button))
			{
				ButtonActions[button]?.Invoke();
			}
		}
	}
}
