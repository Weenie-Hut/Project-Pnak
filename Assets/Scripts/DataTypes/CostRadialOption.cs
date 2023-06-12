using UnityEngine;

namespace Pnak
{
	public abstract class CostRadialOption : CustomCostRadialOption
	{
		[Tooltip("The cost of this option. Use {cost} to insert the cost into the description or title.")]
		public Cost cost;

		public override Cost SetCost(Interactable interactable = null) => cost;
	}

	public abstract class CustomCostRadialOption : RadialOptionSO
	{
		private Cost? previousCost;
		private Interactable previousInteractable;

		public virtual Cost GetCost(Interactable interactable = null)
		{
			if (previousInteractable != interactable || !previousCost.HasValue)
			{
				previousCost = SetCost(interactable);
				previousInteractable = interactable;
			}

			return previousCost.Value;
		}

		public abstract Cost SetCost(Interactable interactable = null);

		public override string Format(string format, Interactable interactable = null)
		{
			return base.Format(format, interactable).FormatById("cost", GetCost(interactable).ToString());
		}

		public override void OnSelect(Interactable interactable = null)
		{
			GameManager.Instance.Charge(GetCost(interactable), interactable);
		}

		public override bool IsSelectable(Interactable interactable = null)
		{
			return GameManager.Instance.CanAfford(GetCost(interactable), interactable);
		}
	}
}