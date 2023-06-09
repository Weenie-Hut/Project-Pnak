using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class HealthBehaviour : StateBehaviour, IDamageReceiver
	{
		[SerializeField]
		private DataSelectorWithOverrides<ResistanceAmount> ResistanceDataSelector = new DataSelectorWithOverrides<ResistanceAmount>();

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
			int selfTypeIndex = Controller.GetBehaviourTypeIndex(this);
			HealthVisualIndex = Controller.FindNetworkMod<HealthVisualMod>(selfTypeIndex, out int scriptIndex);

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

			ResistanceDataSelector.MoveToNext();

			UpdateHealthVisual();
		}

		public void ScaleHealth(float scale)
		{
			_Health *= scale;
			_MaxHealth *= scale;
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
		public bool AddDamage(DamageAmount amount)
		{
			if (!_hasSpawned) return false;

			ResistanceAmount resistance = ResistanceDataSelector.CurrentData;

			// UnityEngine.Debug.Log($"Damage: {amount.PureDamage} {amount.PhysicalDamage} {amount.MagicalDamage} {amount.ApplyModifiers.Length} {resistance.AllMultiplier} {resistance.PhysicalMultiplier} {resistance.MagicalMultiplier}");

			_Health -=
				amount.PureDamage.NaNTo0() * resistance.AllMultiplier.NaNTo0() +
				amount.PhysicalDamage.NaNTo0() * resistance.PhysicalMultiplier.NaNTo0() * resistance.AllMultiplier.NaNTo0() +
				amount.MagicalDamage.NaNTo0() *	resistance.MagicalMultiplier.NaNTo0() * resistance.AllMultiplier.NaNTo0();

			if (_Health <= float.Epsilon)
			{
				OnDeath?.Invoke(this);
				return true;
			}

			foreach (SerializedLiteNetworkedData mod in amount.ApplyModifiers)
			{
				LiteNetworkManager.QueueAddModifier(Controller.NetworkContext, mod);
			}

			if (_Health > _MaxHealth)
				_Health = _MaxHealth;

			ResistanceDataSelector.MoveToNext();
			UpdateHealthVisual();
			return false;
		}

		public void AddOverride(DataOverride<ResistanceAmount> resistance)
		{
			ResistanceDataSelector.AddOverride(resistance);
		}

		public void RemoveOverride(DataOverride<ResistanceAmount> resistance)
		{
			ResistanceDataSelector.RemoveOverride(resistance);
		}

		public void ModifyOverride(DataOverride<ResistanceAmount> resistance)
		{
			ResistanceDataSelector.ModifyOverride(resistance);
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
