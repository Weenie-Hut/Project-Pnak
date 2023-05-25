using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialFolderOption", menuName = "Pnak/Radial SO/Modifier")]
	public class ModifierRadialOption : CostRadialOption
	{
		public Modifier Modifier;

		public override void OnSelect(Interactable interactable = null)
		{
			base.OnSelect(interactable);

			ModifierContainer modContainer = interactable?.GetComponent<ModifierContainer>();

			if (modContainer == null)
			{
				Debug.LogError("ModifierRadialOption.OnSelect: Interactable does not have a ModifierContainer component! Modifier Radial Options should only be used on Interactables with ModifierContainers.");
				return;
			}

			modContainer.RPC_AddModifier(Modifier);
		}
	}
}