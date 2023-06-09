using Fusion;
using UnityEngine;
using System.Collections.Generic;

namespace Pnak
{
	public class LookAtBehaviour : Munition
	{
		[Tooltip("The speed at which the object will rotate to face the target, in degrees per second."), Min(0.01f), Suffix("°/sec")]
		public float RotationSpeed = 120f;
		public float DeltaAngle { get; private set; }
		public InputBehaviourType LookTargetType = InputBehaviourType.Any;

		public override void Initialize()
		{
			base.Initialize();
			DeltaAngle = 180f;
		}

		public override void FixedUpdateNetwork()
		{
			float targetAngle;

			if (Controller.Input.HasValue && (LookTargetType == InputBehaviourType.Any || LookTargetType == InputBehaviourType.PlayerInputOnly))
			{
				targetAngle = Controller.Input.Value.AimAngle;
			}
			else if (LookTargetType == InputBehaviourType.Any || LookTargetType == InputBehaviourType.AutomaticOnly)
			{
				if (CollisionProcessor.ColliderCount == 0)
				{
					DeltaAngle = 180f;
					return;
				}

				Transform target = CollisionProcessor.Colliders[0].transform;
				if (target == null) return;

				Vector3 direction = target.position - transform.position;
				targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			}
			else {
				DeltaAngle = 180f;
				return;
			}

			TransformData transformData = Controller.TransformData;

			float delta = Mathf.DeltaAngle(transformData.RotationAngle, targetAngle);
			float rotationSpeed = RotationSpeed * Runner.DeltaTime;
			float change = Mathf.Clamp(delta, -rotationSpeed, rotationSpeed);
			float newAngle = transformData.RotationAngle + change;

			DeltaAngle = Mathf.Abs(delta - change);

			transformData.RotationAngle = newAngle;
			Controller.TransformData = transformData;
		}
	}
}