using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Place")]
	public class PlaceRadialOption : CostRadialOption
	{
		[Tooltip("The prefab to instantiate on select. Use {prefab} to insert the prefab name into the description or title.")]
		[Required]
		public StateBehaviourController Prefab;

		public override void OnSelect(Interactable interactable = null)
		{
			base.OnSelect(interactable);

			if (!Player.IsValid)
			{
				Debug.LogError("CharacterTypeRadialOption.OnSelect: Player.LocalPlayer is null! This should only be called once the local player has been instantiated.");
				return;
			}

			LiteNetworkManager.RPC_CreateLiteObject(Prefab.PrefabIndex, new TransformData {
				Position = Player.LocalPlayer.Transform.Position,
				RotationAngle = Input.GameInput.Instance.InputData.AimAngle
			});
		}

		public override bool IsSelectable(Interactable interactable = null)
		{
			return base.IsSelectable(interactable) && interactable == null;
		}

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && interactable == null;
		}

		public override string Format(string format)
		{
			return base.Format(format).FormatById("prefab", Prefab?.name);
		}
	}
}