using UnityEngine;
using Fusion;
using System.Runtime.InteropServices;

namespace Pnak
{
	public enum Operation
	{
		AddReloadSpeed,
		MultiplyReloadSpeed,
		AddFireCount,
		MultipleFireCount,
	}

	[System.Serializable, StructLayout(LayoutKind.Sequential)]
	public struct ReloadStateModifierData
	{
		public float Amount;
		public Operation Operation;
	}

	[CreateAssetMenu(fileName = "ReloadStateModifier", menuName = "Pnak/StateModifier/ReloadStateModifier")]
	public class ReloadStateModifierSO : StateModifierSO
	{
		public ReloadStateModifierData BaseData;
		public override StateModifier CreateModifier() => new ReloadStateModifier(BaseData);

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && interactable?.GetComponent<ShootBehaviour>() != null;
		}

		public override string Format(string format)
		{
			return base.Format(format)
				.FormatById("amount", BaseData.Amount)
				.FormatById("operation", BaseData.Operation);
		}
	}

	public class ReloadStateModifier : StateModifier
	{
		private ShootBehaviour shootBehaviour;

		private ReloadStateModifierData Data;
		public ReloadStateModifier(ReloadStateModifierData data) => Data = data;

		public override StateModifier CopyFor(StateBehaviourController controller)
		{
			if (controller.TryGetComponent(out ShootBehaviour shoot))
			{
				ReloadStateModifier modifier = new ReloadStateModifier(Data);
				modifier.shootBehaviour = shoot;
				return modifier;
			}

			return null;
		}

		public override void Added(StateBehaviourController controller)
		{
			base.Added(controller);

			shootBehaviour = controller.GetStateBehaviour<ShootBehaviour>();
			if (shootBehaviour == null) return;

			switch (Data.Operation)
			{
				case Operation.AddReloadSpeed:
					shootBehaviour.IncrementReloadTimes(Data.Amount);
					break;
				case Operation.MultiplyReloadSpeed:
					shootBehaviour.ScaleReloadTimes(Data.Amount);
					break;
				case Operation.AddFireCount:
					shootBehaviour.IncrementFireCount(Data.Amount);
					break;
				case Operation.MultipleFireCount:
					shootBehaviour.ScaleFireCount(Data.Amount);
					break;
			}
		}

		public override void FixedUpdateNetwork()
		{
		}

		public override void Removed()
		{
			if (shootBehaviour == null) return;

			switch (Data.Operation)
			{
				case Operation.AddReloadSpeed:
					shootBehaviour.IncrementReloadTimes(-Data.Amount);
					break;
				case Operation.MultiplyReloadSpeed:
					shootBehaviour.ScaleReloadTimes(1 / Data.Amount);
					break;
				case Operation.AddFireCount:
					shootBehaviour.IncrementFireCount(-Data.Amount);
					break;
				case Operation.MultipleFireCount:
					shootBehaviour.ScaleFireCount(1 / Data.Amount);
					break;
			}
		}
	}
}