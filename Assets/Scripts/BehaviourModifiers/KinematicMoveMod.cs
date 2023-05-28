using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		public struct KinematicMoveData : INetworkStruct
		{
			public float Velocity;
		}

		[FieldOffset(CustomDataOffset)]
		public KinematicMoveData KinematicMove;
	}

	[CreateAssetMenu(fileName = "KinematicMove", menuName = "BehaviourModifier/KinematicMove")]
	public class KinematicMoveMod : LiteNetworkMod
	{
		public class KinematicMoveContext
		{
			public LiteNetworkObject Target;
			public int PositionAndScaleModIndex;
		}

		public override void Initialize(LiteNetworkObject target, in LiteNetworkedData data, out object context)
		{
			context = new KinematicMoveContext
			{
				Target = target,
				PositionAndScaleModIndex = -1
			};
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is KinematicMoveContext moveContext)) return;

			if (moveContext.PositionAndScaleModIndex == -1)
			{
				int transformScriptIndex = LiteNetworkManager.GetIndexOfBehaviour<PositionAndRotationMod>();
				foreach(int modIndex in moveContext.Target.Modifiers)
				{
					if (LiteNetworkManager.GetModifierData(modIndex).ScriptType == transformScriptIndex)
					{
						moveContext.PositionAndScaleModIndex = modIndex;
						break;
					}
				}

				System.Diagnostics.Debug.Assert(((KinematicMoveContext) context).PositionAndScaleModIndex != -1);
				UnityEngine.Debug.Log("MoveContext.Target.Modifiers: " + string.Join(", ", moveContext.Target.Modifiers) + " | moveContext.PositionAndScaleModIndex: " + moveContext.PositionAndScaleModIndex);
			}

			LiteNetworkedData transformData = LiteNetworkManager.GetModifierData(moveContext.PositionAndScaleModIndex);
			Vector2 direction = MathUtil.AngleToDirection(transformData.PositionAndScale.RotationAngle);

			transformData.PositionAndScale.Position += direction * data.KinematicMove.Velocity * SessionManager.Instance.NetworkRunner.DeltaTime;
			LiteNetworkManager.SetModifierData(moveContext.PositionAndScaleModIndex, transformData);
		}

		// public override void OnFixedUpdate(object rContext, ref BehaviourModifierData data)
		// {
		// }

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}
	}
}