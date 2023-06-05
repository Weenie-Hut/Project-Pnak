using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public struct ResistanceAmount
	{
		public float AnyMultiplier;
		public float PhysicalMultiplier;
		public float MagicalMultiplier;
	}

	public class HealthBehaviour : StateBehaviour, IDamageReceiver
	{
		public ResistanceAmount Resistance = new ResistanceAmount
		{
			AnyMultiplier = 1f,
			PhysicalMultiplier = 1f,
			MagicalMultiplier = 1f
		};

		[Tooltip("The starting health of the object.")]
		[SerializeField, Min(0.01f)] private float _SpawnHealth = 1f;
		[Tooltip("The maximum health of the object. If this is lower than the starting health, then this will automatically be set to the starting health.")]
		[SerializeField] private float _SpawnMaxHealth = 1f;
		[Tooltip("If true, the object will be destroyed when it dies. If false, the object will handle dying though callbacks.")]
		[SerializeField] private bool _DespawnOnDeath = true;

		public Action<HealthBehaviour> OnDeath;
		public Action<HealthBehaviour> OnHealthChanged;

		private int HealthVisualIndex = -1;

		private bool _hasSpawned = false;

		private float _Health { get; set; }
		public float Health => _Health;
		private float _MaxHealth { get; set; }
		public float MaxHealth => _MaxHealth;

		public float MoneyOnDeath = 1f;

		public override void Initialize()
		{
			HealthVisualIndex = Controller.FindNetworkMod<HealthVisualMod>(out int scriptIndex);

			var health = _SpawnHealth  * SessionManager.Instance.PlayerCount;
			var max = _SpawnMaxHealth * SessionManager.Instance.PlayerCount;

			if (health > max)
				health = max;

			_Health = health;
			_MaxHealth = max;

			if (_DespawnOnDeath)
				OnDeath += (self) => self.Controller.QueueForDestroy();

			OnDeath += (self) => SpawnerManager.RPC_ChangeMoney(self.MoneyOnDeath);

			_hasSpawned = true;

			UpdateHealthVisual();
		}

		public void UpdateHealthVisual()
		{
			if (HealthVisualIndex >= 0)
			{
				LiteNetworkedData data = LiteNetworkManager.GetModifierData(HealthVisualIndex);
				data.HealthVisual.maxHealth = _MaxHealth;
				data.HealthVisual.currentHealth = _Health;
				LiteNetworkManager.SetModifierData(HealthVisualIndex, data);
			}
		}

		/// <summary>
		/// Adds damage to the object. Returns true if the object is dead.
		/// </summary>
		/// <param name="amount">The amount of damage to add.</param>
		/// <returns>True if the object is dead.</returns>
		public bool AddDamage(DamageAmount amount, List<StateModifier> runtimeModifiers)
		{
			if (!_hasSpawned) return false;

			// TODO: Add armor and resistances
			_Health -=
				amount.PureDamage * Resistance.AnyMultiplier +
				amount.PhysicalDamage * Resistance.PhysicalMultiplier * Resistance.AnyMultiplier +
				amount.MagicalDamage *	Resistance.MagicalMultiplier * Resistance.AnyMultiplier;

			if (_Health <= float.Epsilon)
			{
				OnDeath?.Invoke(this);
				return true;
			}

			for(int i = 0; i < amount.ApplyModifiers.Count; i++)
				Controller.AddStateModifier(amount.ApplyModifiers[i].CreateModifier());

			for (int i = 0; i < runtimeModifiers?.Count; i++)
				Controller.AddStateModifier(runtimeModifiers[i].CopyFor(Controller));

			if (_Health > _MaxHealth)
				_Health = _MaxHealth;

			UpdateHealthVisual();
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

			UpdateHealthVisual();
			return false;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_SpawnHealth > _SpawnMaxHealth)
				_SpawnMaxHealth = _SpawnHealth;
		}

#endif
	}
}
