using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Pilot")]
	public class PilotRadialOption : RadialOptionSO
	{
		public override void OnSelect(Interactable interactable = null)
		{
			if (Player.LocalPlayer == null)
			{
				Debug.LogError("CharacterTypeRadialOption.OnSelect: Player.LocalPlayer is null! This should only be called once the local player has been instantiated.");
				return;
			}

			Tower tower = interactable.GetComponent<Tower>();
			Player.LocalPlayer.RPC_SetPilot(tower.Object, SessionManager.Instance.NetworkRunner.LocalPlayer);

		}

		public override bool IsSelectable(Interactable interactable = null)
		{
			if (Player.LocalPlayer == null)
				return false;
			
			if (interactable == null)
				return false;

			if (!interactable.TryGetComponent<Tower>(out Tower tower))
				return false;

			return tower.Object.InputAuthority.IsNone;
		}
	}
}