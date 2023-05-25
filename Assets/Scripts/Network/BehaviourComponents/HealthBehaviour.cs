using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class HealthBehaviour : NetworkBehaviour, IDamageReceiver
	{
		[Tooltip("The starting health of the object.")]
		[SerializeField, Min(0.01f)] private float _SpawnHealth = 1f;
		[Tooltip("The maximum health of the object. If this is lower than the starting health, then this will automatically be set to the starting health.")]
		[SerializeField] private float _SpawnMaxHealth = 1f;
		[Tooltip("If true, the object will be destroyed when it dies. If false, the object will handle dying though callbacks.")]
		[SerializeField] private bool _DespawnOnDeath = true;

		public Action<HealthBehaviour> OnDeath;
		public Action<HealthBehaviour> OnHealthChanged;

		private bool _hasSpawned = false;

		[Networked(OnChanged = nameof(_OnHealthChanged))] private float _Health { get; set; }
		public float Health => _Health;
		[Networked(OnChanged = nameof(_OnHealthChanged))] private float _MaxHealth { get; set; }
		public float MaxHealth => _MaxHealth;

		public override void Spawned()
		{
			base.Spawned();

			var health = _SpawnHealth  * SessionManager.Instance.PlayerCount;
			var max = _SpawnMaxHealth * SessionManager.Instance.PlayerCount;

			if (health > max)
				health = max;

			_Health = health;
			_MaxHealth = max;

			if (_DespawnOnDeath)
				OnDeath += NetworkExtensions.DespawnSelf;

			_hasSpawned = true;
		}

		/// <summary>
		/// Adds damage to the object. Returns true if the object is dead.
		/// </summary>
		/// <param name="amount">The amount of damage to add.</param>
		/// <returns>True if the object is dead.</returns>
		public bool AddDamage(DamageAmount amount)
		{
			if (!_hasSpawned) return false;

			// TODO: Add armor and resistances
			_Health -= amount.PureDamage + amount.PhysicalDamage + amount.MagicalDamage;

			if (_Health <= float.Epsilon)
			{
				OnDeath?.Invoke(this);
				return true;
			}

			if (_Health > _MaxHealth)
				_Health = _MaxHealth;

			return false;
		}

		/// <summary>
		/// Adds health to the object. Returns true if the object is at max health.
		/// </summary>
		/// <param name="amount">The amount of health to add.</param>
		/// <returns>True if the object is at max health.</returns>
		public bool AddHealth(float amount)
		{
			if (!_hasSpawned) return false;
			
			_Health += amount;
			if (_Health > _MaxHealth)
			{
				_Health = _MaxHealth;
				return true;
			}

			if (_Health <= float.Epsilon)
				OnDeath?.Invoke(this);


			return false;
		}

		private static void _OnHealthChanged(Changed<HealthBehaviour> changed) => changed.Behaviour.OnHealthChanged?.Invoke(changed.Behaviour);

#if UNITY_EDITOR

		private void OnValidate()
		{
			if (_SpawnHealth > _SpawnMaxHealth)
				_SpawnMaxHealth = _SpawnHealth;
		}

#endif
	}
}
