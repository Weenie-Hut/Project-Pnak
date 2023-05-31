using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct KinematicPositionData
		{
			[HideInInspector]
			public Vector2 SpawnPosition;
			[Tooltip("Units per second, where right (positive x) is the forward direction of the network object on spawn.")]
			public Vector2 Velocity;
			[Tooltip("Units per second squared, where right (positive x) is the forward direction of the network object on spawn.")]
			public Vector2 Acceleration;
			[HideInInspector]
			public int SpawnTick;
		}

		[FieldOffset(CustomDataOffset)]
		public KinematicPositionData KinematicMove;
	}

	[CreateAssetMenu(fileName = " KinematicPosition", menuName = "BehaviourModifier/KinematicPosition")]
	public class KinematicPositionMod : TransformMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.KinematicPositionData);

		public override void Initialize(LiteNetworkObject target, in LiteNetworkedData data, out object context)
		{
			base.Initialize(target, in data, out context);
			OnRender(context, data);
		}

		public override void SetTransformData(ref LiteNetworkedData data, TransformData transformData)
		{
			data.KinematicMove.SpawnPosition = transformData.Position;
			data.KinematicMove.Velocity = MathUtil.RotateVector(data.KinematicMove.Velocity, transformData.RotationAngle);
			data.KinematicMove.Acceleration = MathUtil.RotateVector(data.KinematicMove.Acceleration, transformData.RotationAngle);
		}

		public override void SetDefaults(ref LiteNetworkedData data) =>
			SetDefaults(ref data, Vector2.zero);

		public void SetDefaults(ref LiteNetworkedData data, Vector2 spawnPosition, Vector2 velocity = default, Vector2 acceleration = default)
		{
			base.SetDefaults(ref data);

			data.KinematicMove.SpawnPosition = spawnPosition;
			data.KinematicMove.Velocity = velocity;
			data.KinematicMove.Acceleration = acceleration;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);
			data.KinematicMove.SpawnTick = SessionManager.Instance.NetworkRunner.Tick;
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is LiteNetworkObject networkObject)) return;

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;


			float ticksPassed = currentTick - data.KinematicMove.SpawnTick;

			float timePassed = ticksPassed * tickRate;

			Vector2 currentPosition = GetPosition(data, timePassed);

			float transformDirection;

			if (currentTick > data.KinematicMove.SpawnTick + tickRate)
			{
				Vector2 pastPosition = GetPosition(data, timePassed - tickRate);
				transformDirection = MathUtil.DirectionToAngle(currentPosition - pastPosition);
			}
			else {
				transformDirection = MathUtil.DirectionToAngle(data.KinematicMove.Velocity);
			}

			networkObject.Target.transform.SetPositionAndRotation(
				currentPosition,
				Quaternion.Euler(0, 0, transformDirection)
			);
		}

		private Vector2 GetPosition(in LiteNetworkedData data, float timePassed)
		{
			return data.KinematicMove.SpawnPosition + data.KinematicMove.Velocity * timePassed + data.KinematicMove.Acceleration * timePassed * timePassed / 2;
		}
	}
}