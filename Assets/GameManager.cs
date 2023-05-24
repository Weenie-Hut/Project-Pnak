using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System;
using Pnak.Input;

namespace Pnak
{
	public class GameManager : SingletonMono<GameManager>
	{
		public enum ButtonAction
		{
			ToggleMenu,
			MenuButton_1,
			MenuButton_2,
			MenuButton_3,
			MenuButton_4,
			Shoot,
		}

		[Tooltip("The character data to use for each character type. Temporary until we have character prefabs.")]
		public CharacterData[] Characters;

		public NetworkInputData InputData;
		public Vector2? MousePosition { get; private set; }

		public Camera MainCamera { get; private set; }
		public PlayerInput PlayerInput { get; private set;}
		public EventSystem EventSystem { get; private set; }
		public SceneLoader SceneLoader { get; private set; }
		public InputSystemUIInputModule InputSystemUIInputModule { get; private set; }

		public InputMap LoadingInputMap { get; private set; }

		protected override void Awake()
		{
			base.Awake();

			InputData = new NetworkInputData();

			MainCamera = Camera.main;
			DontDestroyOnLoad(MainCamera.gameObject);


			SceneLoader = FindObjectOfType<SceneLoader>();
			PlayerInput = GetComponent<PlayerInput>();
			EventSystem = GetComponent<EventSystem>();
			InputSystemUIInputModule = GetComponent<InputSystemUIInputModule>();

			InputEmulation.SetActionAsset(PlayerInput.actions);

			InputCallbackSystem.SetupInputCallbacks(this, false);
			PlayerInput.onActionTriggered += InputCallbackSystem.OnActionTriggered;

			PlayerInput.uiInputModule = InputSystemUIInputModule;

			LoadingInputMap = InputMap.Menu;
			PlayerInput.SwitchCurrentActionMap(LoadingInputMap.Name());
			InputData.CurrentInputMap = LoadingInputMap;
		}

		public void SetInputMap(InputMap config)
		{

			if (LoadingInputMap == config) return;

			LoadingInputMap = config;
			PlayerInput.SwitchCurrentActionMap(LoadingInputMap.Name());

		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			System.Diagnostics.Debug.Assert(ApplicationWantsToQuit.IsQuitting, "GameManager was destroyed when the application was not quitting. The GameManager should never be destroyed.");
		}

		public NetworkInputData PullNetworkInput()
		{
			SetDynamicInputData();

			var result = InputData;

			InputData.ClearState();
			InputData.CurrentInputMap = LoadingInputMap;

			return result;
		}

		public void SetDynamicInputData()
		{
			// Player Aim
			if (MousePosition.HasValue && Player.LocalPlayer != null)
			{
				InputData.AimDirection = MousePosition.Value - (Vector2)Player.LocalPlayer.transform.position;
			}
		}

		[InputActionTriggered(ActionNames.Move)]
		private void OnMoveTriggered(InputAction.CallbackContext context)
		{
			InputData.Movement = context.ReadValue<Vector2>();
		}

		[InputActionTriggered(ActionNames.ControllerAim)]
		private void OnControllerAimTriggered(InputAction.CallbackContext context)
		{
			InputData.AimDirection = context.ReadValue<Vector2>();
			MousePosition = null;
		}

		[InputActionTriggered(ActionNames.MouseAim)]
		private void OnMouseAimTriggered(InputAction.CallbackContext context)
		{
			if (Player.LocalPlayer == null)
				return;

			MousePosition = MainCamera.ScreenToWorldPoint(context.ReadValue<Vector2>());
		}

		[InputActionTriggered(ActionNames.Shoot)]
		private void OnShootTriggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(1, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.PlaceTower)]
		private void OnPlaceTowerTriggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(2, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.Menu_Button1)]
		private void OnMenuButton1Triggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(1, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.Menu_Button2)]
		private void OnMenuButton2Triggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(2, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.Menu_Button3)]
		private void OnMenuButton3Triggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(3, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.Menu_Button4)]
		private void OnMenuButton4Triggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown(4, context.ReadValueAsButton());
		}
	}
}
