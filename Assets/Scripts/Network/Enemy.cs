using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Enemy : NetworkBehaviour
	{
		[SerializeField] private float _DefaultMovementSpeed = 1f;

		[Networked] private Vector3 _TargetPosition { get; set; }
		public Vector3 TargetPosition => _TargetPosition;
		[Networked] private float _MovementSpeed { get; set; }
		public float MovementSpeed => _MovementSpeed;
		private byte PathIndex = 1;
		private Transform path;

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

		public override void Spawned()
		{
			base.Spawned();

			_MovementSpeed = _DefaultMovementSpeed;

			if (path != null)
			{
				SetNextPosition();
			}
		}

		public void Init(Transform path)
		{
			this.path = path;
		}

		public override void FixedUpdateNetwork()
		{
			float movement = _MovementSpeed * Runner.DeltaTime;
			transform.position = Vector3.MoveTowards(transform.position, _TargetPosition, movement);

			if (path != null)
				if (Vector3.Distance(transform.position, _TargetPosition) < 0.1f)
					SetNextPosition();
		}

		[Pnak.Input.InputActionTriggered("Shoot")]
		public void TestingShoot(UnityEngine.InputSystem.InputAction.CallbackContext context)
		{
			UnityEngine.Debug.Log("Shoot");
		}
	}
}
