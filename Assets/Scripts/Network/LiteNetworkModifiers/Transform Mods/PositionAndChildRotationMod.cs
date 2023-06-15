using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct PositionAndChildRotationData 
		{
			[HideInInspector]
			public Vector2 Position;
			[HideInInspector]
			public float CurrentChildRotationAngle;
		}

		[FieldOffset(CustomDataOffset)]
		public PositionAndChildRotationData PositionAndChildRotation;
	}

	[CreateAssetMenu(fileName = "PositionAndChildRotation", menuName = "BehaviourModifier/PositionAndChildRotation")]
	public class PositionAndChildRotationMod : TransformMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.PositionAndChildRotationData);

		[SerializeField, Min(0)] private int ChildIndex = 0;

		public class PositionAndChildRotationContext
		{
			public LiteNetworkObject networkObject;
			public Transform childTransform;
		}

		public override void SetTransformData(ref LiteNetworkedData data, TransformData transformData)
		{
			data.PositionAndChildRotation.Position = transformData.Position;
			data.PositionAndChildRotation.CurrentChildRotationAngle = transformData.RotationAngle;
		}

		public override TransformData GetTransformData(object context, in LiteNetworkedData data)
		{
			return new TransformData
			{
				Position = data.PositionAndChildRotation.Position,
				RotationAngle = data.PositionAndChildRotation.CurrentChildRotationAngle
			};
		}

		public override void Initialize(LiteNetworkObject target, in LiteNetworkedData data, out object context)
		{
			Transform targetTrans = target.Target.transform;
			context = new PositionAndChildRotationContext
			{
				networkObject = target,
				childTransform = ChildIndex < targetTrans.childCount ? targetTrans.GetChild(ChildIndex) : null
			};

			OnRender(context, data);
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is PositionAndChildRotationContext selfContext))
			{
				UnityEngine.Debug.LogError("Context is not the correct type: " + context.GetType() + " != " + typeof(PositionAndChildRotationContext) + ". Object: " + context.ToString());
				return;
			}

			selfContext.networkObject.Target.transform.position = data.PositionAndChildRotation.Position;
			if (selfContext.childTransform != null)
			{
				selfContext.childTransform.rotation = Quaternion.Euler(0, 0, data.PositionAndChildRotation.CurrentChildRotationAngle);
			}
		}
	}
}