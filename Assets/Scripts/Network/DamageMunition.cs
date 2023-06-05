using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Pnak
{
	public class DamageMunition : Munition
	{
		[Tooltip("Damage dealt to each target hit, in order.")]
		public List<DamageAmount> DamageByPeirce = new List<DamageAmount> { new DamageAmount { PureDamage = 1f } };
		[Tooltip("If true, the projectile will despawn when it hits targets equal to the length of DamageByPeirce. If false, the projectile will keep looping through DamageByPeirce.")]
		public bool CappedPeirce = true;
		[Tooltip("The number of targets the projectile can hit before despawning. If CappedPeirce is false, this value is ignored.")]
		[HideIf(nameof(CappedPeirce), true)]
		public int Peirce = 1;
		[Tooltip("If true, the projectile will ignore targets after the first hit. Disable for DoT while colliding effects.")]
		public bool IgnoreAfterFirstHit = true;

		public int PeirceCount { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			PeirceCount = 0;
		}

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			if (CappedPeirce && PeirceCount >= Peirce)
				return; // Already hit max number of targets. Wait for despawn

			DamageAmount damage = DamageByPeirce[PeirceCount];
			if (!IgnoreAfterFirstHit)
				damage *= Runner.DeltaTime;

			CollisionProcessor.ApplyDamage(collider2D, damage, Controller.StateModifiers);

			PeirceCount++;
			if (CappedPeirce)
			{
				if (PeirceCount >= Peirce)
				{
					Controller.QueueForDestroy();
					return;
				}
			}
			else if (PeirceCount >= Peirce)
			{
				PeirceCount = 0;
			}

			if (IgnoreAfterFirstHit)
				CollisionProcessor.IgnoreCollider(collider2D);
		}

		internal void IncrementDamage(DamageAmount amount)
		{
			for (int i = 0; i < DamageByPeirce.Count; i++)
			{
				DamageByPeirce[i] = DamageByPeirce[i] + amount;
			}
		}

		public void ScaleDamage(DamageAmount amount)
		{
			for (int i = 0; i < DamageByPeirce.Count; i++)
			{
				DamageByPeirce[i] = DamageByPeirce[i] * amount;
			}
		}

		public void AddApplyModifiers(List<StateModifierSO> modifiers)
		{
			for (int i = 0; i < DamageByPeirce.Count; i++)
			{
				DamageByPeirce[i].ApplyModifiers.AddRange(modifiers);
			}
		}

		public void RemoveApplyModifiers(List<StateModifierSO> modifiers)
		{
			for (int i = 0; i < DamageByPeirce.Count; i++)
			{
				for (int j = 0; j < modifiers.Count; j++)
				{
					DamageByPeirce[i].ApplyModifiers.Remove(modifiers[j]);
				}
			}
		}
	}
}