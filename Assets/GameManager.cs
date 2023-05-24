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

		public readonly static Dictionary<ControllerConfig, string> ControllerConfigNames = new Dictionary<ControllerConfig, string>()
		{
			{ ControllerConfig.Gameplay, "Gameplay" },
			{ ControllerConfig.Menu, "Menu" },
		};

		[Tooltip("The character data to use for each character type. Temporary until we have character prefabs.")]
		public CharacterData[] Characters;

		public NetworkInputData InputData;
		public Vector2? MousePosition { get; private set; }

		public Camera MainCamera { get; private set; }
		public PlayerInput PlayerInput { get; private set;}
		public EventSystem EventSystem { get; private set; }
		public SceneLoader SceneLoader { get; private set; }
		public InputSystemUIInputModule InputSystemUIInputModule { get; private set; }

		public ControllerConfig LoadingConfig { get; private set; }

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

			InputCallbackSystem.RegisterInputCallbacks(this, false);
			PlayerInput.onActionTriggered += InputCallbackSystem.OnActionTriggered;

			PlayerInput.uiInputModule = InputSystemUIInputModule;

			LoadingConfig = ControllerConfig.Menu;
			PlayerInput.SwitchCurrentActionMap(ControllerConfigNames[LoadingConfig]);
			InputData.ControllerConfig = LoadingConfig;
		}

		public void SetControllerConfig(ControllerConfig config)
		{
			string actionMapName = ControllerConfigNames[config];
			if (PlayerInput.currentActionMap.name != actionMapName)
				PlayerInput.SwitchCurrentActionMap(actionMapName);

			LoadingConfig = config;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			System.Diagnostics.Debug.Assert(ApplicationWantsToQuit.IsQuitting, "GameManager was destroyed when the application was not quitting. The GameManager should never be destroyed.");
		}

		public NetworkInputData PullNetworkInput()
		{
			if (InputData.ControllerConfig != LoadingConfig)
			{
				if (InputData.ControllerConfig == ControllerConfig.Gameplay)
					ClearGameplayInputs();
			}

			SetDynamicInputData();

			var result = InputData;

			

			InputData.ClearButtons();
			InputData.ControllerConfig = LoadingConfig;

			return result;
		}

		public void SetDynamicInputData()
		{
			// Player Aim
			if (MousePosition.HasValue && Player.LocalPlayer != null)
			{
				InputData.AimDirection = MousePosition.Value - (Vector2)Player.LocalPlayer.transform.position;
				MousePosition = null;
			}
		}

		public void ClearGameplayInputs()
		{
			InputData.movement = Vector2.zero;
		}

		[InputActionTriggered(ActionNames.Move)]
		private void OnMoveTriggered(InputAction.CallbackContext context)
		{
			InputData.movement = context.ReadValue<Vector2>();
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

		[InputActionTriggered(ActionNames.Shoot, InputStateFilters.PreformedThisFrame)]
		private void OnShootTriggered(InputAction.CallbackContext context)
		{
			InputData.Button1Pressed = true;
		}

		[InputActionTriggered(ActionNames.PlaceTower, InputStateFilters.PreformedThisFrame)]
		private void OnPlaceTowerTriggered(InputAction.CallbackContext context)
		{
			InputData.Button2Pressed = true;
		}

		[InputActionTriggered(ActionNames.Menu_Button1, InputStateFilters.PreformedThisFrame)]
		private void OnMenuButton1Triggered(InputAction.CallbackContext context)
		{
			InputData.Button3Pressed = true;
		}

		[InputActionTriggered(ActionNames.Menu_Button2, InputStateFilters.PreformedThisFrame)]
		private void OnMenuButton2Triggered(InputAction.CallbackContext context)
		{
			InputData.Button4Pressed = true;
		}

		[InputActionTriggered(ActionNames.Menu_Button3, InputStateFilters.PreformedThisFrame)]
		private void OnMenuButton3Triggered(InputAction.CallbackContext context)
		{
			InputData.Button5Pressed = true;
		}

		[InputActionTriggered(ActionNames.Menu_Button4, InputStateFilters.PreformedThisFrame)]
		private void OnMenuButton4Triggered(InputAction.CallbackContext context)
		{
			InputData.Button6Pressed = true;
		}
	}
}
