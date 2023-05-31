using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct StaticTransformData : INetworkStruct
		{
			[HideInInspector]
			public Vector3 Position;
			[HideInInspector]
			public Vector2 Scale;
			[HideInInspector]
			public float RotationAngle;

			public Vector2 PositionXY
			{
				get => Position;
				set {
					Position.x = value.x;
					Position.y = value.y;
				}
			}
		}

		[FieldOffset(CustomDataOffset)]
		public StaticTransformData Transform;
	}

	[CreateAssetMenu(fileName = "StaticTransform", menuName = "BehaviourModifier/StaticTransform")]
	public class StaticTransformMod : TransformMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.StaticTransformData);

		public override void SetTransformData(ref LiteNetworkedData data, TransformData transformData)
		{
			data.Transform.Position = transformData.Position;
			if (transformData.Scale == Vector2.zero) transformData.Scale = Vector2.one;
			else data.Transform.Scale = transformData.Scale;
			data.Transform.RotationAngle = transformData.RotationAngle;
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is LiteNetworkObject netContext)) return;


			netContext.Target.transform.SetPositionAndRotation(
				data.Transform.Position,
				Quaternion.Euler(0, 0, data.Transform.RotationAngle)
			);
			netContext.Target.transform.localScale = data.Transform.Scale;
		}
	}
}