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

		public override void FixedInitialize()
		{
			base.FixedInitialize();
			DeltaAngle = 180f;
		}

		public override void InputFixedUpdateNetwork()
		{
			base.InputFixedUpdateNetwork();

			if (LookTargetType == InputBehaviourType.AutomaticOnly)
				return;
			
			if (Controller.Input.HasValue)
				UpdateTransform(Controller.Input.Value.AimAngle);
		}

		public override void FixedUpdateNetwork()
		{
			if (Controller.Input.HasValue && LookTargetType != InputBehaviourType.AutomaticOnly)
				return;

			if (LookTargetType == InputBehaviourType.PlayerInputOnly || CollisionProcessor.ColliderCount == 0)
			{
				DeltaAngle = 180f;
				return;
			}

			Transform target = CollisionProcessor.Colliders[0].transform;
			if (target == null) return;

			Vector3 direction = target.position - transform.position;
			UpdateTransform(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
		}

		private void UpdateTransform(float targetAngle)
		{
			TransformData transformData = Controller.TransformCache;

			float delta = Mathf.DeltaAngle(transformData.RotationAngle, targetAngle);
			float rotationSpeed = RotationSpeed * Runner.DeltaTime;
			float change = Mathf.Clamp(delta, -rotationSpeed, rotationSpeed);
			float newAngle = transformData.RotationAngle + change;

			DeltaAngle = Mathf.Abs(delta - change);

			transformData.RotationAngle = newAngle;
			Controller.TransformCache.Value = transformData;
		}
	}
}