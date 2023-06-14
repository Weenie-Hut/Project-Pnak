using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Pnak
{
	public class DamageMunition : Munition
	{
		public enum DamageIntervalType
		{
			[Tooltip("Damage is applied every frame if a target is available.")]
			None = 0,
			[Tooltip("Damage is applied in a pulsing fashion, with the interval being the time between each pulse.")]
			Pulsing = 1,
			[Tooltip("Damage is applied as soon as a target becomes available, with the interval being the minimum time between each damage.")]
			Cooldown = 2,
			[Tooltip("Damage is applied as soon as a target becomes available, with the interval being the minimum time between each damage. The interval is calculated per object, meaning each object will take damage as soon as it is able, rather than being on the same timer. Interval is treats like invulnerable frames rather than a collective effect.")]
			CooldownPerTarget = 3
		}

		[SerializeField]
		private DataSelectorWithOverrides<DamageAmount> DamageDataSelector = new DataSelectorWithOverrides<DamageAmount>();

		[Button("SetMaxValue","Inf Peirce", "Set the peirce to the maximum value, essentially making the projectile never despawn due to peirce cap.", nameof(Peirce), uint.MaxValue)]
		[Tooltip("The number of targets the projectile can hit before despawning. Use the button to max out the peirce so the projectile will never despawn."), MinMax(min: 1u)]
		public uint Peirce = 1;

		[Tooltip("If true, the projectile will ignore targets after the first hit. Disable for DoT while colliding effects.")]
		public bool IgnoreAfterFirstHit = true;

		[Tooltip("The type of interval to use when applying damage. None will apply damage every frame. Pulsing will apply damage every IntervalBetweenHits seconds. Cooldown will apply damage as soon as a target becomes available, with the interval being the minimum time between each damage. CooldownPerTarget is the same as Cooldown, but the interval is calculated per object, meaning each object will take damage as soon as it is able, rather than being on the same timer. Interval is treats like invulnerable frames rather than a collective effect.")]
		[Validate(nameof(ValidateIntervalType))]
		public DamageIntervalType IntervalType = DamageIntervalType.None;

		[Tooltip("The time between each time damage is done. If less than runner.DeltaTime, damage will be applied every frame to every overlapping target.")]
		[ShowIf(nameof(IntervalType))]
		[Suffix("sec"), MinMax(min: 0.0333f)]
		public float IntervalBetweenHits = 0.33f;

		[HideIf(nameof(intervalInTicks), -1)]
		[AsLabel(LabelType.Italic | LabelType.Right, "Damage is in d/sec and dealt every {0} network tick(s)")]
		[SerializeField]
		private int intervalInTicks; // Used for any interval

		[Tooltip("If true, the damage data will be cycled after each hit. Otherwise, if multiple targets are hit on a single frame, the same damage data will be used for each target.")]
		public bool PickDamageDataAfterEachHit = false;

		private int lastIntervalTick = -1;
		public SortedList<int, Collider2D> IgnoreCollidersByFrame; // Used for cooldowns per target

		public uint HitCount { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			HitCount = 0;

			if (IntervalType != DamageIntervalType.None && intervalInTicks <= 0)
			{
				UnityEngine.Debug.LogError("IntervalType is set to " + IntervalType + " but IntervalBetweenHits is not greater than 0. IntervalType will be set to None.");
				IntervalType = DamageIntervalType.None;
			}
			
			switch (IntervalType)
			{
				case DamageIntervalType.None:
					if (IgnoreAfterFirstHit)
						intervalInTicks = -1;
					else intervalInTicks = 1;
					break;
				case DamageIntervalType.CooldownPerTarget:
					IgnoreCollidersByFrame = new SortedList<int, Collider2D>();
					break;
			}
		}

		private void AdjustCurrentForInterval()
		{
			if (intervalInTicks == -1) return;
			DamageDataSelector.CurrentData.Scale((SessionManager.Instance.NetworkRunner.DeltaTime * intervalInTicks));
		}

		private void MoveToNext()
		{
			if (DamageDataSelector.MoveToNext())
				AdjustCurrentForInterval();
		}

		public void AddOverride(DataOverride<DamageAmount> dataOverride)
		{
			DamageDataSelector.AddOverride(dataOverride);
			AdjustCurrentForInterval();
		}

		public void RemoveOverride(DataOverride<DamageAmount> dataOverride)
		{
			DamageDataSelector.RemoveOverride(dataOverride);
			AdjustCurrentForInterval();
		}

		public void ModifyOverride(DataOverride<DamageAmount> dataOverride)
		{
			DamageDataSelector.ModifyOverride(dataOverride);
			AdjustCurrentForInterval();
		}

		public override void FixedInitialize()
		{
			base.FixedInitialize();
			MoveToNext();
		}


		public override void FixedUpdateNetwork()
		{
			switch (IntervalType)
			{
				case DamageIntervalType.Pulsing:
					if (lastIntervalTick + intervalInTicks > SessionManager.Tick)
						return;
					lastIntervalTick = SessionManager.Tick;
					break;
				case DamageIntervalType.Cooldown:
					if (lastIntervalTick + intervalInTicks > SessionManager.Tick)
						return;
					break;
				case DamageIntervalType.CooldownPerTarget:
					while (IgnoreCollidersByFrame.Count > 0 && IgnoreCollidersByFrame.Keys[0] + intervalInTicks <= SessionManager.Tick)
					{
						CollisionProcessor.ClearIgnoredCollider(IgnoreCollidersByFrame.Values[0]);
						IgnoreCollidersByFrame.RemoveAt(0);
					}
					break;
			}

			uint previousHitCount = HitCount;

			base.FixedUpdateNetwork(); // Calls OnHit for each collider

			if (HitCount > previousHitCount)
			{
				if (!PickDamageDataAfterEachHit)
					MoveToNext();

				if (IntervalType == DamageIntervalType.Cooldown)
					lastIntervalTick = SessionManager.Tick;
			}
		}

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			if (HitCount >= Peirce)
			{
				UnityEngine.Debug.LogError("Munition has or is trying to hit more targets than allowed: " + HitCount + " >= " + Peirce + ". The munition should be be queued for destruction which happens automatically to prevent this call.");
				LiteNetworkManager.QueueDeleteLiteObject(Controller.NetworkContext);
				return;
			}

			// UnityEngine.Debug.Log("Munition hit " + collider2D.name + " at " + SessionManager.Tick + " with " + DamageDataSelector.CurrentData);
			CollisionProcessor.ApplyDamage(collider2D, DamageDataSelector.CurrentData);

			HitCount++;
			if (HitCount >= Peirce)
			{
				Controller.QueueForDestroy();
				return;
			}

			if (PickDamageDataAfterEachHit)
				MoveToNext();

			if (IgnoreAfterFirstHit)
				CollisionProcessor.IgnoreCollider(collider2D);
			else
			{
				switch (IntervalType)
				{
					case DamageIntervalType.CooldownPerTarget:
						IgnoreCollidersByFrame.Add(SessionManager.Tick, collider2D);
						break;
				}
			}
		}

		private bool ValidateIntervalType()
		{
			return !IgnoreAfterFirstHit || IntervalType != DamageIntervalType.CooldownPerTarget;
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			if (Peirce == 0) Peirce = 1;

			if (IgnoreAfterFirstHit && IntervalType == DamageIntervalType.None)
				intervalInTicks = -1;
			else if (!IgnoreAfterFirstHit && IntervalType == DamageIntervalType.None)
			{
				intervalInTicks = 1;
			}
			else intervalInTicks = Mathf.RoundToInt(IntervalBetweenHits / SessionManager.DeltaTime);
		}
	}
}