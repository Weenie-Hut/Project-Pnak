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
		public Vector2? MouseScreenPosition { get; private set; }
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
			SetDynamicInputData(InputData.CurrentInputMap);

			var result = InputData;

			InputData.ClearState();
			InputData.CurrentInputMap = LoadingInputMap;

			return result;
		}

		public void SetDynamicInputData(InputMap useMap)
		{
			// Player Aim
			if (MouseScreenPosition.HasValue)
			{
				if (useMap == InputMap.Gameplay && Player.IsValid)
				{
					Vector2 mouseWorld = GameManager.Instance.MainCamera.ScreenToWorldPoint(MouseScreenPosition.Value);
					InputData.AimDirection = mouseWorld - (Vector2)Player.LocalPlayer.Transform.Position;
				}
				else if (useMap == InputMap.Menu)
				{
					InputData.AimDirection = MouseScreenPosition.Value - new Vector2(Screen.width / 2f, Screen.height / 2f);
				}
			}
		}

		public void SetInputMap(InputMap config)
		{
			if (LoadingInputMap == config) return;

			LoadingInputMap = config;

			// This makes sure that the action map does not change before all actions have been triggered.
			// UnityEngine.Debug.Log("SetInputMap: " + config.Name());
			InputCallbackSystem.OnLateActionTriggered += LateUpdateMap;
		}

		private void LateUpdateMap(InputAction.CallbackContext context)
		{
			// UnityEngine.Debug.Log("LateUpdateMap: " + LoadingInputMap.Name());
			InputCallbackSystem.OnLateActionTriggered -= LateUpdateMap;
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
			MouseScreenPosition = null;
		}

		[InputActionTriggered(ActionNames.MousePosition)]
		private void OnMouseAimTriggered(InputAction.CallbackContext context)
		{
			MouseScreenPosition = context.ReadValue<Vector2>();
		}

		[InputActionTriggered(ActionNames.Shoot)]
		private void OnShootTriggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown((int)NetworkButton.Primary, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.PlaceTower)]
		private void OnPlaceTowerTriggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown((int)NetworkButton.Secondary, context.ReadValueAsButton());
		}

		[InputActionTriggered(ActionNames.Testing)]
		private void OnTestingTriggered(InputAction.CallbackContext context)
		{
			InputData.SetButtonDown((int)NetworkButton.Space, context.ReadValueAsButton());
		}
	}
}