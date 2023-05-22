using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LifetimeBehaviour : NetworkBehaviour
	{
		[Tooltip("The starting Lifetime of the object.")]
		[SerializeField, Min(0.01f)] private float _SpawnLifetime = 1f;
		[Tooltip("The maximum Lifetime of the object. If this is lower than the starting health, then this will be automatically set to the starting Lifetime.")]
		[SerializeField] private float _SpawnMaxLifetime = 1f;
		[Tooltip("If true, the object will be destroyed when Lifetime expires. If false, the object will handle dying though callbacks.")]
		[SerializeField] private bool _DespawnOnExpire = true;

		public Action<LifetimeBehaviour> OnExpire;
		public Action<LifetimeBehaviour> OnLifetimeChanged;

		[Networked(OnChanged = nameof(_OnLifetimeChanged))] private TickTimer _Lifetime { get; set; }
		public float Lifetime => _Lifetime.RemainingTime(Runner) ?? 0f;
		[Networked(OnChanged = nameof(_OnLifetimeChanged))] private float _MaxLifetime { get; set; }
		public float MaxLifetime => _MaxLifetime;

		public override void Spawned()
		{
			base.Spawned();

			var life = _SpawnLifetime;
			var max = _SpawnMaxLifetime;

			_Lifetime = TickTimer.CreateFromSeconds(Runner, life);
			_MaxLifetime = max;

			if (_DespawnOnExpire)
				OnExpire += NetworkExtensions.DespawnSelf;
		}

		/// <summary>
		/// Adds damage to the object. Returns true if the object is dead.
		/// </summary>
		/// <param name="amount">The amount of damage to add.</param>
		/// <returns>True if the object is dead.</returns>
		public bool RemoveLifetime(float amount)
		{
			float time = _Lifetime.RemainingTime(Runner) ?? 0f;
			time -= amount;

			if (time <= float.Epsilon)
			{
				_Lifetime = TickTimer.None;
				OnExpire?.Invoke(this);
				return true;
			}

			if (time > _MaxLifetime)
				time = _MaxLifetime;
			_Lifetime = TickTimer.CreateFromSeconds(Runner, time);

			return false;
		}

		/// <summary>
		/// Adds health to the object. Returns true if the object is at max health.
		/// </summary>
		/// <param name="amount">The amount of health to add.</param>
		/// <returns>True if the object is at max health.</returns>
		public bool AddLifetime(float amount)
		{
			float time = _Lifetime.RemainingTime(Runner) ?? 0f;
			time += amount;

			if (time <= float.Epsilon)
			{
				_Lifetime = TickTimer.None;
				OnExpire?.Invoke(this);
				return false;
			}

			if (time > _MaxLifetime)
				time = _MaxLifetime;

			_Lifetime = TickTimer.CreateFromSeconds(Runner, time);

			if (time == _MaxLifetime)
				return true;
			return false;
		}

		public override void FixedUpdateNetwork()
		{
			if (_Lifetime.ExpiredOrNotRunning(Runner))
			{
				_Lifetime = TickTimer.None;
				OnExpire?.Invoke(this);
			}
		}

		private static void _OnLifetimeChanged(Changed<LifetimeBehaviour> changed) => changed.Behaviour.OnLifetimeChanged?.Invoke(changed.Behaviour);

#if UNITY_EDITOR

		private void OnValidate()
		{
			if (_SpawnLifetime > _SpawnMaxLifetime)
				_SpawnMaxLifetime = _SpawnLifetime;
		}

#endif
	}
}
