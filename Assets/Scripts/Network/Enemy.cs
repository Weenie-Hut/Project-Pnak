using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Enemy : StateBehaviour
	{
		[SerializeField] private float _DefaultMovementSpeed = 1f;

		private Vector3 _TargetPosition { get; set; }
		public Vector3 TargetPosition => _TargetPosition;
		private float _MovementSpeed { get; set; }
		public float MovementSpeed => _MovementSpeed;
		private byte PathIndex = 1;
		private Transform path;

		public void SetNextPosition()
		{
			if (PathIndex >= path.childCount)
			{
				Controller.QueueForDestroy();
				return;
			}

			var target = Random.Range(0, path.GetChild(PathIndex).childCount);
			_TargetPosition = path.GetChild(PathIndex).GetChild(target).position;

			if (PathIndex >= path.childCount)
			{
				Controller.QueueForDestroy();
			}

			PathIndex++;
		}

		public override void Initialize()
		{
			_MovementSpeed = _DefaultMovementSpeed;
		}

		public void Init(Transform path)
		{
			this.path = path;

			if (path != null)
			{
				SetNextPosition();
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (path == null)
			{
				UnityEngine.Debug.LogError("Path is null!  This object must be initialized with custom values using the callback.");
				return;
			}

			float movement = _MovementSpeed * Runner.DeltaTime;

			TransformData transformData = Controller.TransformData;
			transformData.Position = Vector3.MoveTowards(transformData.Position, _TargetPosition, movement);

			if (Vector3.Distance(transformData.Position, _TargetPosition) < 0.1f)
				SetNextPosition();
	
			Controller.TransformData = transformData;
		}
	}
}
