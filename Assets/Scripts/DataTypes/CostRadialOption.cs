using UnityEngine;

namespace Pnak
{
	public abstract class CostRadialOption : RadialOptionSO
	{
		public Cost cost;

		public override void OnSelect(Interactable interactable = null)
		{
			GameManager.Instance.Charge(cost, interactable);
		}

		public override bool IsSelectable(Interactable interactable = null)
		{
			return GameManager.Instance.CanAfford(cost, interactable);
		}
	}
}