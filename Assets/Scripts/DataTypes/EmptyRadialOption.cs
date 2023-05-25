using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "RadialOption", menuName = "Pnak/Radial SO/Empty")]
	public class EmptyRadialOption : RadialOptionSO
	{
		public override void OnSelect(Interactable _ = null)
		{
			throw new System.Exception("EmptyRadialOption should never be selected! It should only be used as a placeholder for empty slots in a RadialMenu.");
		}
	}
}