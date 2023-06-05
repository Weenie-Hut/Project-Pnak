using UnityEngine;
using Fusion;
using System.Runtime.InteropServices;

namespace Pnak
{
	public enum DamageOperation
	{
		Add,
		Multiply
	}

	[System.Serializable, StructLayout(LayoutKind.Sequential)]
	public struct DamageStateModifierData
	{
		public DamageAmount Amount;
		public DamageOperation Operation;
	}

	[CreateAssetMenu(fileName = "DamageStateModifier", menuName = "Pnak/StateModifier/DamageStateModifier")]
	public class DamageStateModifierSO : StateModifierSO
	{
		public DamageStateModifierData BaseData;
		public override StateModifier CreateModifier() => new DamageStateModifier(BaseData);

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && (interactable?.GetComponent<ShootBehaviour>() != null || interactable?.GetComponent<DamageMunition>() != null);
		}

		public override string Format(string format)
		{
			return base.Format(format)
				.FormatById("amount", BaseData.Amount)
				.FormatById("operation", BaseData.Operation);
		}
	}

	public class DamageStateModifier : StateModifier
	{
		private DamageMunition damageBehaviour;

		private DamageStateModifierData Data;
		public DamageStateModifier(DamageStateModifierData data) => Data = data;

		public override StateModifier CopyFor(StateBehaviourController controller)
		{
			if (controller.TryGetComponent(out DamageMunition shoot))
			{
				DamageStateModifier modifier = new DamageStateModifier(Data);
				modifier.damageBehaviour = shoot;
				return modifier;
			}

			return null;
		}

		public override void Added(StateBehaviourController controller)
		{
			base.Added(controller);

			damageBehaviour = controller.GetStateBehaviour<DamageMunition>();
			if (damageBehaviour == null) return;

			switch (Data.Operation)
			{
				case DamageOperation.Add:
					damageBehaviour.IncrementDamage(Data.Amount);
					break;
				case DamageOperation.Multiply:
					damageBehaviour.ScaleDamage(Data.Amount);
					break;
			}

			damageBehaviour.AddApplyModifiers(Data.Amount.ApplyModifiers);
		}

		public override void FixedUpdateNetwork()
		{
		}

		public override void Removed()
		{
			if (damageBehaviour == null) return;

			switch (Data.Operation)
			{
				case DamageOperation.Add:
					damageBehaviour.IncrementDamage(-Data.Amount);
					break;
				case DamageOperation.Multiply:
					damageBehaviour.ScaleDamage(1 / Data.Amount);
					break;
			}

			damageBehaviour.RemoveApplyModifiers(Data.Amount.ApplyModifiers);
		}
	}
}