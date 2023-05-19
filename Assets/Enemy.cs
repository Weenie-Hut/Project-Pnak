using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Enemy : NetworkBehaviour
	{
		public static List<Enemy> Enemies = new List<Enemy>();

		[Networked] private float Health { get; set; }

		private byte PathIndex { get; set; }
		[Networked] private Vector3 _targetPosition { get; set; }

		private Transform path;

		private float speed;

		public bool Damage(float amount)
		{
			Health -= amount;
			if (Health <= float.Epsilon)
			{
				Runner.Despawn(Object);
				Enemies.Remove(this);
				return true;
			}

			return false;
		}

		public void SetNextPosition()
		{
			var target = Random.Range(0, path.GetChild(PathIndex).childCount);
			if (Object.HasInputAuthority)
				_targetPosition = path.GetChild(PathIndex).GetChild(target).position;

			if (PathIndex >= path.childCount)
			{
				Runner.Despawn(Object);
			}

			PathIndex++;
		}

		public void Init(Transform path, float speed, float health)
		{
			this.path = path;
			this.speed = speed;
			Health = health;

			SetNextPosition();
			transform.position = _targetPosition;
			SetNextPosition();

			Enemies.Add(this);
		}

		public override void FixedUpdateNetwork()
		{
			float movement = speed * Runner.DeltaTime;
			transform.position = Vector3.MoveTowards(transform.position, _targetPosition, movement);

			if (Object.HasInputAuthority)
			{
				if (Vector3.Distance(transform.position, _targetPosition) < 0.1f)
					SetNextPosition();
			}
		}
	}
}
