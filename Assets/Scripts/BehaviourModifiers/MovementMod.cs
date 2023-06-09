using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable, StructLayout(LayoutKind.Explicit)]
		public struct MovementData
		{
			/***** Copy of Duration and Visuals ******/
			[Suffix("sec"), Tooltip("The duration of this upgrade. Use 0 or less for permanent upgrades.")]
			[Default(float.PositiveInfinity), Min(0)]
			[FieldOffset(0)]
			public float Duration;
			[HideInInspector]
			[FieldOffset(4)]
			public int startTick;
			/***********************************/
			[EnumNameSuffix(typeof(Priorities))]
			[FieldOffset(8)]
			public ushort Priority;
			[AsEnum(typeof(ValueStackingType))]
			[FieldOffset(10)]
			public byte StackingType;
			[FieldOffset(11)]
			[NaNButton, Suffix("units/sec")] public float MovementSpeed;
			[FieldOffset(15)]
			[NaNButton, Min(0.0f), Suffix("sec")] public float HoldDuration;

			public bool IsEqual(in MovementData other)
			{
				return MovementSpeed.Equals(other.MovementSpeed) &&
					HoldDuration.Equals(other.HoldDuration) &&
					StackingType.Equals(other.StackingType) &&
					Priority.Equals(other.Priority);
			}
		}


		[FieldOffset(CustomDataOffset)]
		public MovementData Movement;
	}

	[CreateAssetMenu(fileName = "Movement", menuName = "BehaviourModifier/Movement")]
	public class MovementMod : DurationAndVisualsMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.MovementData);

		public class MovementContext : DurationAndVisualsContext
		{
			public Enemy Enemy;
			public DataOverride<MovementAmount> Override;
			public LiteNetworkedData.MovementData PreviousData;

			public MovementContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				Enemy = networkContext.Target.GetStateBehaviour<Enemy>();
			}
		}

		public override void SetDefaults(ref LiteNetworkedData data)
		{
			base.SetDefaults(ref data);
			data.Movement.StackingType = (byte)ValueStackingType.Multiply;
			data.Movement.Priority = (ushort)Priorities.GeneralMult;
			data.Movement.MovementSpeed = float.NaN;
			data.Movement.HoldDuration = float.NaN;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			MovementContext _context = new MovementContext(networkContext);
			context = _context;

			if (!SessionManager.IsServer) return;
			ResetOverride(_context, in data);
		}

		public void ResetOverride(MovementContext context, in LiteNetworkedData data)
		{
			if (context.Enemy == null) return;

			if (context.Override == null)
				context.Override = new DataOverride<MovementAmount> { Data = new MovementAmount() };

			context.Override.Data.MovementSpeed = data.Movement.MovementSpeed;
			context.Override.Data.HoldDuration = data.Movement.HoldDuration;
			context.Override.Priority = data.Movement.Priority;
			context.Override.StackingType = (ValueStackingType)data.Movement.StackingType;

			context.Enemy.ModifyOverride(context.Override);
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is MovementContext MovementContext)) return;
			if (MovementContext.PreviousData.IsEqual(data.Movement)) return;
			MovementContext.PreviousData = data.Movement;

			ResetOverride(MovementContext, in data);
		}

		public override void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnInvalidatedUpdate(rContext, ref data);

			if (!(rContext is MovementContext MovementContext)) return;

			if (MovementContext.Enemy != null)
			{
				MovementContext.Enemy.RemoveOverride(MovementContext.Override);
			}
		}
	}
}