using UnityEngine;

namespace Pnak
{
	public class PlayerAgent : StateBehaviour
	{
		public float Speed = 1f;
		public float MP_Max = 60f;
		public float MP_RegenerationRate = 1f;
		public RadialOptionSO[] InteractionOptions;

		[SerializeField] private Transform _AimGraphic;
		[SerializeField] private StateBehaviour[] DisableWhenBusy;

		public override void InputFixedUpdateNetwork()
		{
			base.InputFixedUpdateNetwork();

			if (Controller.Input.HasValue)
			{
				Vector2 movement = Controller.Input.Value.Movement * Speed;

				TransformData transformData = Controller.TransformCache;
				transformData.Position += (Vector3)movement * Runner.DeltaTime;
				transformData.RotationAngle = Controller.Input.Value.AimAngle;
				Controller.TransformCache.Value = transformData;
			}
		}

		public override void Render()
		{
			if (enabled == false || Controller.InputAuthority != SessionManager.LocalPlayer)
			{
				_AimGraphic.gameObject.SetActive(false);
				return;
			}
			else if (_AimGraphic.gameObject.activeSelf == false)
			{
				_AimGraphic.gameObject.SetActive(true);
			}

			if (LevelUI.Exists)
			{
				LevelUI.Instance.MPBar.RawValueRange = new Vector2(0.0f, MP_Max);
				LevelUI.Instance.MPBar.NormalizedValue = Player.LocalPlayer.MPPercent;
			}

			// _AimGraphic.rotation = Quaternion.Euler(0.0f, 0.0f, Input.GameInput.Instance.InputData.AimAngle);
		}

		public void SetPilotState(bool isPiloting)
		{
			foreach(StateBehaviour behave in DisableWhenBusy)
			{
				behave.enabled = !isPiloting;
			}
		}

		public void SetPilotGraphics(bool isPiloting)
		{
			foreach(Transform child in transform)
			{
				if (child != _AimGraphic)
					child.gameObject.SetActive(!isPiloting);
			}
		}
	}
}