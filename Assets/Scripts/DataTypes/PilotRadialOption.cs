using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Pilot")]
	public class PilotRadialOption : RadialOptionSO
	{
		public override void OnSelect(Interactable interactable = null)
		{
			if (!Player.IsValid)
			{
				Debug.LogError("CharacterTypeRadialOption.OnSelect: Player.LocalPlayer is null! This should only be called once the local player has been instantiated.");
				return;
			}

			StateBehaviourController tower = interactable.GetComponent<StateBehaviourController>();
			Player.LocalPlayer.RPC_SetPilot(
				tower.TargetNetworkIndex,
				SessionManager.Instance.NetworkRunner.LocalPlayer);

		}

		public override bool IsSelectable(Interactable interactable = null)
		{
			if (Player.IsValid)
				return false;
			
			if (interactable == null)
				return false;

			if (!interactable.TryGetComponent<StateBehaviourController>(out StateBehaviourController tower))
				return false;

			return !tower.InputAuthority.IsNone;
		}
	}
}