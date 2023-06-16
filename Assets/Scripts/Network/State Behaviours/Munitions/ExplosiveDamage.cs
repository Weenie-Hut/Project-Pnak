using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Pnak
{
	public class ExplosiveDamage : DamageMunition
	{
		[Header("Explosion Settings")]
		[Tooltip("Length in seconds that the explosion will last. After this time, the explosion will be disabled and no longer do damage. Set to Infinity to disable this feature.")]
		[SerializeField, MinMax(0.0333f, float.PositiveInfinity)]
		[Button("SetPosInf", "Infinity", "Set the duration to infinity.")]
		private float ExplosionDamageDuration = float.PositiveInfinity;

		[AsLabel(LabelType.Italic | LabelType.Right, "Explosion will last {0} network tick(s)")]
		[HideIf(nameof(IsDurationInfinite))]
		[SerializeField] private int durationInTicks;


		[Tooltip("If true, the explosion will use a damage curve to do damage based on the distance from the center of the explosion. Otherwise, the damage will be constant.")]
		[SerializeField, Validate(nameof(ValidateUseDamageCurve))]
		private bool UseDamageCurve = false;

		[Tooltip("The damage to do based on the distance from the center of the explosion.")]
		[ShowIf(nameof(UseDamageCurve))]
		[SerializeField] private AnimationCurve DamageCurve = AnimationCurve.Linear(0, 1, 1, 0);

		[Tooltip("The maximum distance that the explosion will effect. The DamageCurve will be evaluated as normalized distance.")]
		[ShowIf(nameof(UseDamageCurve)), MinMax(0.01f)]
		[SerializeField, Button(nameof(CalculateMaxDistanceByCollider), "Collider Extends", "Calculate the maximum distance based on the collider extents. This is only an estimate and may not be accurate for all colliders.")]
		private float maxDistance = float.NegativeInfinity;

		[Tooltip("The maximum distance that modifier effects will still be applied.")]
		[ShowIf(nameof(UseDamageCurve))]
		[SerializeField, Range(0, 1)]
		private float NormMaxApplyDistance = 1f;

#if UNITY_EDITOR
		[AsLabel(LabelType.Italic | LabelType.Right, "Modifiers will apply at any distance less than {0} units.")]
		[ShowIf(nameof(UseDamageCurve))]
		[HideIf(nameof(NormMaxApplyDistance), 1f)]
		[SerializeField]
		private float __MaxApplyDistance__ = 0f;

		[AsLabel(LabelType.Bold | LabelType.Center, "No explosion data, use 'DamageMunition' instead.")]
		[HideIf(true), SerializeField]
		private bool __HasExplosionData__ = false;
#endif

		private void CalculateMaxDistanceByCollider()
		{
			if (CollisionProcessor?._Collider == null) maxDistance = float.NegativeInfinity;
			else maxDistance = CollisionProcessor._Collider.bounds.extents.magnitude;
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (IsDurationInfinite()) return;
			durationInTicks--;

			if (durationInTicks <= 0)
			{
				enabled = false;
			}
		}

		protected override void ApplyDamage(Collider2D collider2D, float? distance, DamageAmount damageAmount)
		{
#if DEBUG
			if (UseDamageCurve && distance == null)
			{
				Debug.LogError($"ExplosiveDamage {name} ({GetType().Name}) is configured to use a damage curve but the CollisionProcessor is not configured to calculate distances. Falling back to constant damage. This will throw an error in a build.");
				UseDamageCurve = false;
			}
#endif

			if (UseDamageCurve)
			{
				float normDistance = Mathf.Clamp01(distance.Value / maxDistance);
				float scale = DamageCurve.Evaluate(normDistance);

				if (scale != 1 || normDistance > NormMaxApplyDistance)
				{
					damageAmount = damageAmount.Copy();
					damageAmount.Scale(scale);
					
					if (normDistance > NormMaxApplyDistance)
					{
						damageAmount.ApplyModifiers = null;
					}
				}
			}

			base.ApplyDamage(collider2D, distance, damageAmount);
		}

		protected override void OnValidate()
		{
			base.OnValidate();

#if UNITY_EDITOR
			__HasExplosionData__ = !IsDurationInfinite() || UseDamageCurve;

			if (!IsDurationInfinite()) durationInTicks = Mathf.RoundToInt(ExplosionDamageDuration / SessionManager.DeltaTime);

			if (CollisionProcessor?._Collider == null) return;
			if (float.IsNegativeInfinity(maxDistance)) CalculateMaxDistanceByCollider();

			__MaxApplyDistance__ = maxDistance * NormMaxApplyDistance;
#endif
		}

		private bool ValidateUseDamageCurve()
		{
			if (UseDamageCurve == false) return true;
			if (CollisionProcessor?._Collider == null) return false;

			return CollisionProcessor.CalculateDistances;
		}

		private bool IsDurationInfinite() => float.IsPositiveInfinity(ExplosionDamageDuration);
	}
}