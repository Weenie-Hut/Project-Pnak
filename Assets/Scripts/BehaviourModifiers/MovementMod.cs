using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public enum MovementStackIds
	{
		SourShot = 3040,
	}

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
			public ushort OverridePriority;
			[AsEnum(typeof(ValueStackingType))]
			[FieldOffset(10)]
			public byte OverrideStackingType;
			[FieldOffset(11)]
			[NaNButton, Suffix("units/sec")] public float MovementSpeed;
			[FieldOffset(15)]
			[NaNButton, Min(0.0f), Suffix("sec")] public float HoldDuration;
			[Tooltip("The stack ID of this Resistance. If a new Resistance is applied to a target with the same stack ID, the old Resistance will be stacked with the new one."), EnumNameSuffix(typeof(MovementStackIds))]
			[FieldOffset(19)]
			public short StackId;
			[AsEnum(typeof(ValueStackingType))]
			[FieldOffset(21)]
			public byte DurationStackType;
			[HideIf(nameof(DurationStackType), 0)]
			[AsEnum(typeof(ValueStackingType)), Default(1)]
			[FieldOffset(22)]
			public byte AmountStackType;

			public bool IsEqual(in MovementData other)
			{
				return MovementSpeed.Equals(other.MovementSpeed) &&
					HoldDuration.Equals(other.HoldDuration) &&
					OverrideStackingType.Equals(other.OverrideStackingType) &&
					OverridePriority.Equals(other.OverridePriority);
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
			data.Movement.OverrideStackingType = (byte)ValueStackingType.Multiply;
			data.Movement.OverridePriority = (ushort)Priorities.GeneralMult;
			data.Movement.MovementSpeed = float.NaN;
			data.Movement.HoldDuration = float.NaN;
			data.Movement.DurationStackType = (byte)ValueStackingType.Replace;
			data.Movement.AmountStackType = (byte)ValueStackingType.Replace;
		}

		public override bool ModAdded_CombineWith(object rContext, ref LiteNetworkedData current, in LiteNetworkedData next)
		{
			if (ScriptIndex != next.ScriptType) return false;
			if (current.Movement.StackId != next.Movement.StackId) return false;
			if (current.Movement.DurationStackType == (byte)ValueStackingType.DoNotStack) return false;
			if (current.Movement.AmountStackType == (byte)ValueStackingType.DoNotStack) return false;
			if (current.Movement.DurationStackType != next.Movement.DurationStackType) return false;
			if (current.Movement.AmountStackType != next.Movement.AmountStackType) return false;

			current.DoT.Duration = ValueStack.Stack(current.Movement.Duration, next.Movement.Duration, (ValueStackingType)current.Movement.DurationStackType);
			current.Movement.MovementSpeed = ValueStack.Stack(current.Movement.MovementSpeed, next.Movement.MovementSpeed, (ValueStackingType)current.Movement.AmountStackType);

			return true;
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
			context.Override.Priority = data.Movement.OverridePriority;
			context.Override.StackingType = (ValueStackingType)data.Movement.OverrideStackingType;

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