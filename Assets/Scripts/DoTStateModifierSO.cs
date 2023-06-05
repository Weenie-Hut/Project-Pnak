using UnityEngine;
using Fusion;
using System.Runtime.InteropServices;

namespace Pnak
{
	[System.Serializable, StructLayout(LayoutKind.Sequential)]
	public struct DoTStateModifierData
	{
		[Suffix("damage/sec")]
		public DamageAmount Damage;

		[Header("On Health Settings"), Suffix("sec")]
		public ValueStackSettings<float> h_Duration;
		public StackSettings h_DamageStacking;
		[Suffix("sec")]
		[Tooltip("The time between each tick of damage. If less than runner.DeltaTime, damage will be applied every frame.")]
		public ValueStackSettings<float> Interval;
		[Tooltip("If false, the modifier will apply as soon as it is added. If true, the modifier will wait until the first interval to apply.")]
		public bool StartWithInterval;

		// [Header("On Damage Munition")]
		// None? No stacking? Munition modifiers should only be applied once, so stacking is not necessary.

		[Header("On Shoot Behaviour"), Suffix("sec")]
		public ValueStackSettings<float> s_Duration;
		public StackSettings s_DamageStacking;
	}

	[CreateAssetMenu(fileName = "DoTStateModifier", menuName = "Pnak/StateModifier/DoTStateModifier")]
	public class DoTStateModifierSO : StateModifierSO
	{
		public DoTStateModifierData BaseData;

		public override StateModifier CreateModifier() => new DoTStateModifier(BaseData);

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && (
				interactable?.GetComponent<ShootBehaviour>() != null ||
				interactable?.GetComponent<DamageMunition>() != null ||
				interactable?.GetComponent<HealthBehaviour>() != null
			);
		}

		public override string Format(string format)
		{
			return base.Format(format)
				.FormatById("damage", BaseData.Damage);
		}
	}

	public class DoTStateModifier : StateModifier
	{
		private HealthBehaviour healthBehaviour;
		private ShootBehaviour shootBehaviour;

		private StackableDamage Damage;
		private StackableFloat Interval;

		private int intervalInTicks;

		private DamageAmount cachedDamage;

		private DoTStateModifierData Data;
		public DoTStateModifier(DoTStateModifierData data) => Data = data;

		public override StateModifier CopyFor(StateBehaviourController controller)
		{
			if (controller.GetComponent<ShootBehaviour>() != null ||
				controller.GetComponent<DamageMunition>() != null ||
				controller.GetComponent<HealthBehaviour>() != null)
				return new DoTStateModifier(Data);

			return null;
		}

		public override bool TryStackWith(StateModifier other)
		{
			if (!(other is DoTStateModifier otherDoT)) return false;

			bool somethingStacked = false;

			if (healthBehaviour != null)
			{
				somethingStacked |= ResetStartTick(Duration.StackWith(GetTimeLeft(), otherDoT.Data.h_Duration));
				somethingStacked |= Damage.StackWith(Data.Damage, otherDoT.Data.Damage, otherDoT.Data.h_DamageStacking.StackId, otherDoT.Data.h_DamageStacking.StackingType);
				somethingStacked |= Interval.StackWith(Data.Interval, otherDoT.Data.Interval);
				SetTickInterval();
			}
			else if (shootBehaviour != null)
			{
				somethingStacked |= ResetStartTick(Duration.StackWith(GetTimeLeft(), otherDoT.Data.s_Duration));
				somethingStacked |= Damage.StackWith(Data.Damage, otherDoT.Data.Damage, otherDoT.Data.s_DamageStacking.StackId, otherDoT.Data.s_DamageStacking.StackingType);
			}

			return somethingStacked;
		}

		private void SetTickInterval()
		{
			intervalInTicks = Mathf.RoundToInt(Data.Interval / SessionManager.Instance.NetworkRunner.DeltaTime);
			cachedDamage = Damage.Value * (SessionManager.Instance.NetworkRunner.DeltaTime * intervalInTicks);
		}

		public override void Added(StateBehaviourController controller)
		{
			base.Added(controller);

			healthBehaviour = controller.GetStateBehaviour<HealthBehaviour>();
			shootBehaviour = controller.GetStateBehaviour<ShootBehaviour>();
			if (healthBehaviour != null)
			{
				Damage = new StackableDamage {
					Value = Data.Damage,
					StackingType = Data.h_DamageStacking.StackingType,
					StackId = Data.h_DamageStacking.StackId
				};
				SetDuration(Data.h_Duration);
				SetTickInterval();
				if (Data.StartWithInterval)
					tickCounter = intervalInTicks;
				else tickCounter = 0;
			}
			else if (shootBehaviour != null)
			{
				Damage = new StackableDamage {
					Value = Data.Damage,
					StackingType = Data.s_DamageStacking.StackingType,
					StackId = Data.s_DamageStacking.StackId
				};
				SetDuration(Data.s_Duration);
			}
		}

		private int tickCounter;
		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (healthBehaviour == null) return;

			if (tickCounter > 0)
			{
				tickCounter--;
				return;
			}

			healthBehaviour.AddDamage(cachedDamage, null);
			tickCounter = intervalInTicks;
		}

		public override void Removed()
		{
		}
	}
}