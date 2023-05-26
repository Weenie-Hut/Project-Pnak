using Fusion;
using UnityEngine;

namespace Pnak
{
	public class BulletProjectile : Projectile
	{
		[Tooltip("Damage dealt to each target hit, in order.")]
		[SerializeField] private DamageAmount[] _DamageByPeirce = new DamageAmount[] { new DamageAmount { PureDamage = 1f } };
		[Tooltip("If true, the projectile will despawn when it hits targets equal to the length of DamageByPeirce. If false, the projectile will keep looping through DamageByPeirce.")]
		[SerializeField] private bool CappedPeirce = true;
		[Tooltip("If true, the projectile will ignore targets after the first hit. Disable for DoT while colliding effects.")]
		[SerializeField] private bool IgnoreAfterFirstHit = true;

		[Networked] private int _PeirceRemaining { get; set; }
		[Networked] private float _DamageModifier { get; set; }
		public int PeirceRemaining => _PeirceRemaining;

		public override void Spawned()
		{
			base.Spawned();

			_PeirceRemaining = _DamageByPeirce.Length;
		}

		public override void Initialize(ModifierContainer modifiers)
		{
			_DamageModifier = 1;
			if (modifiers == null) return;

			foreach (var mod in modifiers.GetModifiersOfType(ModifierTarget.Damage))
			{
				_DamageModifier = mod.ApplyValue(_DamageModifier);
			}
		}

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			DamageAmount damage = _DamageByPeirce[_PeirceRemaining - 1] * _DamageModifier;
			if (!IgnoreAfterFirstHit)
				damage *= Runner.DeltaTime;
			if (!CollisionProcessor.ApplyDamage(collider2D, damage))
				return; // Did not apply damage, so don't peirce
		
			_PeirceRemaining--;
			if (CappedPeirce)
			{
				if (_PeirceRemaining <= 0)
				{
					Despawn();
					return;
				}
			}
			else
			{
				if (_PeirceRemaining <= 0)
					_PeirceRemaining = _DamageByPeirce.Length;
			}

			if (IgnoreAfterFirstHit)
				CollisionProcessor.IgnoreCollider(collider2D);
		}
	}
}