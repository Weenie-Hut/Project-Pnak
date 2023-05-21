using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Enemy : NetworkBehaviour
	{
		public static List<Enemy> Enemies = new List<Enemy>();

		[SerializeField] private SpriteFillBar _HealthBar;
		[SerializeField] private float _DefaultHealth = 1f;
		[SerializeField] private float _DefaultMovementSpeed = 1f;

		[Networked(OnChanged = nameof(OnHealthChanged))] private float _Health { get; set; }
		public float Health => _Health;
		[Networked(OnChanged = nameof(OnHealthChanged))] private float _MaxHealth { get; set; }
		public float MaxHealth => _MaxHealth;
		[Networked] private Vector3 _TargetPosition { get; set; }
		public Vector3 TargetPosition => _TargetPosition;
		[Networked] private float _MovementSpeed { get; set; }
		public float MovementSpeed => _MovementSpeed;
		private byte PathIndex = 1;
		private Transform path;

		public bool Damage(float amount)
		{
			_Health -= amount;
			if (_Health <= float.Epsilon)
			{
				Runner.Despawn(Object);
				Enemies.Remove(this);
				return true;
			}

			return false;
		}

		public static void OnHealthChanged(Changed<Enemy> changed) => changed.Behaviour.OnHealthChanged();
		private void OnHealthChanged() => _HealthBar.Value = _Health / _MaxHealth;

		public void SetNextPosition()
		{
			if (PathIndex >= path.childCount)
			{
				Runner.Despawn(Object);
				return;
			}

			var target = Random.Range(0, path.GetChild(PathIndex).childCount);
			_TargetPosition = path.GetChild(PathIndex).GetChild(target).position;

			if (PathIndex >= path.childCount)
			{
				Runner.Despawn(Object);
			}

			PathIndex++;
		}

		public void Init(Transform path, float speedScale, float healthScale)
		{
			this.path = path;
			_MovementSpeed = _DefaultMovementSpeed * speedScale;
			_MaxHealth = _Health = _DefaultHealth * healthScale;

			SetNextPosition();
			Enemies.Add(this);
		}

		public override void FixedUpdateNetwork()
		{
			float movement = _MovementSpeed * Runner.DeltaTime;
			transform.position = Vector3.MoveTowards(transform.position, _TargetPosition, movement);

			if (Vector3.Distance(transform.position, _TargetPosition) < 0.1f)
				SetNextPosition();
		}
	}
}
