using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Pnak.Input
{
	[RequireComponent(typeof(PlayerInput))]
	[RequireComponent(typeof(InputSystemUIInputModule))]
	[RequireComponent(typeof(EventSystem))]
	public class GameInput : SingletonMono<GameInput>
	{
		public NetworkInputData InputData;
		public Vector2? MousePosition { get; private set; }
		public PlayerInput PlayerInput { get; private set; }

		public InputSystemUIInputModule InputSystemUIInputModule { get; private set; }
		public InputMap LoadingInputMap { get; private set; }
		public EventSystem EventSystem { get; private set; }

		protected override void Awake()
		{
			base.Awake();

			InputData = new NetworkInputData();
			
			PlayerInput = GetComponent<PlayerInput>();
			InputSystemUIInputModule = GetComponent<InputSystemUIInputModule>();
			EventSystem = GetComponent<EventSystem>();

			InputEmulation.SetActionAsset(PlayerInput.actions);
			InputCallbackSystem.SetupInputCallbacks(this, false);
			PlayerInput.onActionTriggered += InputCallbackSystem.OnActionTriggered;

			PlayerInput.uiInputModule = InputSystemUIInputModule;

			LoadingInputMap = InputMap.Menu;
			PlayerInput.SwitchCurrentActionMap(LoadingInputMap.Name());
			InputData.CurrentInputMap = LoadingInputMap;
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

		public void SetInputMap(InputMap config)
		{
			if (LoadingInputMap == config) return;

			LoadingInputMap = config;
			PlayerInput.SwitchCurrentActionMap(LoadingInputMap.Name());
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

			MousePosition = GameManager.Instance.MainCamera.ScreenToWorldPoint(context.ReadValue<Vector2>());
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