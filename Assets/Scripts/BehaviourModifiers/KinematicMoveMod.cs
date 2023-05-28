using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct BehaviourModifierData
	{
		public struct KinematicMoveData : INetworkStruct
		{
			public float Velocity;
		}

		[FieldOffset(CustomDataOffset)]
		public KinematicMoveData KinematicMove;
	}

	[CreateAssetMenu(fileName = "KinematicMove", menuName = "BehaviourModifier/KinematicMove")]
	public class KinematicMoveMod : BehaviourModifier
	{
		public class KinematicMoveContext
		{
			public NetworkObjectContext Target;
			public int PositionAndScaleModIndex;
		}

		public override void Initialize(NetworkObjectContext target, in BehaviourModifierData data, out object context)
		{
			context = new KinematicMoveContext
			{
				Target = target,
				PositionAndScaleModIndex = -1
			};
		}

		public override void OnRender(object context, in BehaviourModifierData data)
		{
			if (!(context is KinematicMoveContext moveContext)) return;

			if (moveContext.PositionAndScaleModIndex == -1)
			{
				int transformScriptIndex = BehaviourModifierManager.Instance.GetIndexOfBehaviour<PositionAndRotationMod>();
				foreach(int modIndex in moveContext.Target.Modifiers)
				{
					if (GetModifierData(modIndex).ScriptType == transformScriptIndex)
					{
						moveContext.PositionAndScaleModIndex = modIndex;
						break;
					}
				}

				System.Diagnostics.Debug.Assert(((KinematicMoveContext) context).PositionAndScaleModIndex != -1);
				UnityEngine.Debug.Log("MoveContext.Target.Modifiers: " + string.Join(", ", moveContext.Target.Modifiers) + " | moveContext.PositionAndScaleModIndex: " + moveContext.PositionAndScaleModIndex);
			}

			BehaviourModifierData transformData = GetModifierData(moveContext.PositionAndScaleModIndex);
			Vector2 direction = MathUtil.AngleToDirection(transformData.PositionAndScale.RotationAngle);

			transformData.PositionAndScale.Position += direction * data.KinematicMove.Velocity * SessionManager.Instance.NetworkRunner.DeltaTime;
			SetModifierData(moveContext.PositionAndScaleModIndex, transformData);
		}

		// public override void OnFixedUpdate(object rContext, ref BehaviourModifierData data)
		// {
		// }

		public override void OnInvalidatedRender(object context, in BehaviourModifierData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}
	}
}