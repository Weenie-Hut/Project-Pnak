using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct StaticTransformData 
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
		public StaticTransformData StaticTransform;
	}

	[CreateAssetMenu(fileName = "StaticTransform", menuName = "BehaviourModifier/StaticTransform")]
	public class StaticTransformMod : TransformMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.StaticTransformData);

		public override void SetTransformData(ref LiteNetworkedData data, TransformData transformData)
		{
			data.StaticTransform.Position = transformData.Position;
			data.StaticTransform.Scale = transformData.Scale;
			data.StaticTransform.RotationAngle = transformData.RotationAngle;
		}

		public override TransformData GetTransformData(object context, in LiteNetworkedData data)
		{
			return new TransformData
			{
				Position = data.StaticTransform.Position,
				Scale = data.StaticTransform.Scale,
				RotationAngle = data.StaticTransform.RotationAngle
			};
		}

		public override void Initialize(LiteNetworkObject target, in LiteNetworkedData data, out object context)
		{
			base.Initialize(target, in data, out context);
			OnRender(context, data);
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is LiteNetworkObject netContext)) return;

			netContext.Target.transform.SetPositionAndRotation(
				data.StaticTransform.Position,
				Quaternion.Euler(0, 0, data.StaticTransform.RotationAngle)
			);
			netContext.Target.transform.localScale = new Vector3(data.StaticTransform.Scale.x, data.StaticTransform.Scale.y, 1);
		}
	}
}