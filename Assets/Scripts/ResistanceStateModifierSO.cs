using UnityEngine;
using Fusion;
using System.Runtime.InteropServices;

namespace Pnak
{
	[System.Serializable, StructLayout(LayoutKind.Sequential)]
	public struct ResistanceStateModifierData
	{
		public ResistanceAmount Resistance;

		[Header("On Health Settings"), Suffix("sec")]
		public ValueStackSettings<float> h_Duration;
		public StackSettings h_ResistanceStacking;

		// [Header("On Damage Munition")]
		// None? No stacking? Munition modifiers should only be applied once, so stacking is not necessary.

		[Header("On Shoot Behaviour"), Suffix("sec")]
		public ValueStackSettings<float> s_Duration;
		public StackSettings s_ResistanceStacking;
	}

	[CreateAssetMenu(fileName = "ResistanceStateModifier", menuName = "Pnak/StateModifier/ResistanceStateModifier")]
	public class ResistanceStateModifierSO : StateModifierSO
	{
		public ResistanceStateModifierData BaseData;

		public override StateModifier CreateModifier() => new ResistanceStateModifier(BaseData);

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && (
				interactable?.GetComponent<HealthBehaviour>() != null
			);
		}

		public override string Format(string format)
		{
			return base.Format(format)
				.FormatById("pure", BaseData.Resistance.AnyMultiplier)
				.FormatById("physical", BaseData.Resistance.PhysicalMultiplier)
				.FormatById("magical", BaseData.Resistance.MagicalMultiplier);
		}
	}

	public class ResistanceStateModifier : StateModifier
	{
		private HealthBehaviour healthBehaviour;
		private ShootBehaviour shootBehaviour;

		private StackableResistance Resistance;
		private StackableFloat Interval;

		private int intervalInTicks;

		private ResistanceStateModifierData Data;
		public ResistanceStateModifier(ResistanceStateModifierData data) => Data = data;

		public override StateModifier CopyFor(StateBehaviourController controller)
		{
			if (controller.GetComponent<ShootBehaviour>() != null ||
				controller.GetComponent<DamageMunition>() != null ||
				controller.GetComponent<HealthBehaviour>() != null)
				return new ResistanceStateModifier(Data);

			return null;
		}

		public override bool TryStackWith(StateModifier other)
		{
			if (!(other is ResistanceStateModifier otherDoT)) return false;

			bool somethingStacked = false;

			if (healthBehaviour != null)
			{
				UnApply();
				somethingStacked |= ResetStartTick(Duration.StackWith(GetTimeLeft(), otherDoT.Data.h_Duration));
				somethingStacked |= Resistance.StackWith(Data.Resistance, otherDoT.Data.Resistance, otherDoT.Data.h_ResistanceStacking.StackId, otherDoT.Data.h_ResistanceStacking.StackingType);
				Apply();
			}
			else if (shootBehaviour != null)
			{
				somethingStacked |= ResetStartTick(Duration.StackWith(GetTimeLeft(), otherDoT.Data.s_Duration));
				somethingStacked |= Resistance.StackWith(Data.Resistance, otherDoT.Data.Resistance, otherDoT.Data.s_ResistanceStacking.StackId, otherDoT.Data.s_ResistanceStacking.StackingType);
			}

			return somethingStacked;
		}

		public override void Added(StateBehaviourController controller)
		{
			base.Added(controller);

			healthBehaviour = controller.GetComponent<HealthBehaviour>();
			shootBehaviour = controller.GetComponent<ShootBehaviour>();

			if (healthBehaviour != null)
			{
				Resistance = new StackableResistance {
					Value = Data.Resistance,
					StackingType = Data.h_ResistanceStacking.StackingType,
					StackId = Data.h_ResistanceStacking.StackId
				};
				SetDuration(Data.h_Duration);
				Apply();
			}
			else if (shootBehaviour != null)
			{
				Resistance = new StackableResistance {
					Value = Data.Resistance,
					StackingType = Data.s_ResistanceStacking.StackingType,
					StackId = Data.s_ResistanceStacking.StackId
				};
				SetDuration(Data.s_Duration);
			}
		}

		private void Apply()
		{
			ResistanceAmount resistance = healthBehaviour.Resistance;
			resistance.AnyMultiplier *= Data.Resistance.AnyMultiplier;
			resistance.PhysicalMultiplier *= Data.Resistance.PhysicalMultiplier;
			resistance.MagicalMultiplier *= Data.Resistance.MagicalMultiplier;

			healthBehaviour.Resistance = resistance;
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();
		}

		public override void Removed()
		{
			if (healthBehaviour == null) return;
			UnApply();
		}

		private void UnApply()
		{
			ResistanceAmount resistance = healthBehaviour.Resistance;
			resistance.AnyMultiplier /= Data.Resistance.AnyMultiplier;
			resistance.PhysicalMultiplier /= Data.Resistance.PhysicalMultiplier;
			resistance.MagicalMultiplier /= Data.Resistance.MagicalMultiplier;

			healthBehaviour.Resistance = resistance;
		}
	}
}