using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct BehaviourModifierData
	{
		public struct PositionAndScaleData : INetworkStruct
		{
			private Vector3 data;

			public Vector2 Position
			{
				get => data;
				set {
					data.x = value.x;
					data.y = value.y;
				}
			}

			public float RotationAngle
			{
				get => data.z;
				set => data.z = value;
			}
		}

		[FieldOffset(CustomDataOffset)]
		public PositionAndScaleData PositionAndScale;
	}

	[CreateAssetMenu(fileName = "PositionAndScale", menuName = "BehaviourModifier/PositionAndScale")]
	public class PositionAndRotationMod : BehaviourModifier
	{
		public override void OnRender(object context, in BehaviourModifierData data)
		{
			if (!(context is NetworkObjectContext netContext)) return;


			netContext.Target.transform.SetPositionAndRotation(
				new Vector3(
					data.PositionAndScale.Position.x,
					data.PositionAndScale.Position.y,
					netContext.Target.transform.position.z),
				Quaternion.Euler(0, 0, data.PositionAndScale.RotationAngle)
			);
		}
	}
}