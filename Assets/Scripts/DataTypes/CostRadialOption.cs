using UnityEngine;

namespace Pnak
{
	public abstract class CostRadialOption : RadialOptionSO
	{
		[Tooltip("The cost of this option. Use {cost} to insert the cost into the description or title.")]
		public Cost cost;

		public override string Format(string format)
		{
			return base.Format(format).FormatById("cost", cost.ToString());
		}

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