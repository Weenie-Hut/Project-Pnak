using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LinearMoveBehaviour : NetworkBehaviour
	{
		[SerializeField] private float SpawnMovementSpeed;

		private float _movementSpeed;

		private Vector3 direction;

		public override void Spawned()
		{
			base.Spawned();

			_movementSpeed = SpawnMovementSpeed;

			float angle = transform.localEulerAngles.z;
			direction = MathUtil.AngleToDirection(angle);
		}

		public override void FixedUpdateNetwork()
		{
			transform.position += _movementSpeed * direction * Runner.DeltaTime;
		}
	}
}