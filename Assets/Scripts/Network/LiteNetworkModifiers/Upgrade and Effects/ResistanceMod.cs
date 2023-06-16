using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public enum ResistanceStackIds
	{
		RedPepperSpicyBlend = 2520,
	}

	public partial struct LiteNetworkedData
	{
		[System.Serializable, StructLayout(LayoutKind.Explicit)]
		public struct ResistanceData
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
			public byte OverrideType;
			[FieldOffset(11)]
			[NaNButton, Min(0f)] public float AllDamageAmount;
			[FieldOffset(15)]
			[NaNButton, Min(0f)] public float PhysicalDamageAmount;
			[FieldOffset(19)]
			[NaNButton, Min(0f)] public float MagicalDamageAmount;
			[Tooltip("The stack ID of this Resistance. If a new Resistance is applied to a target with the same stack ID, the old Resistance will be stacked with the new one."), EnumNameSuffix(typeof(ResistanceStackIds))]
			[FieldOffset(23)]
			public short StackId;
			[AsEnum(typeof(ValueStackingType))]
			[FieldOffset(25)]
			public byte DurationStackType;
			[HideIf(nameof(DurationStackType), 0)]
			[AsEnum(typeof(ValueStackingType)), Default(1)]
			[FieldOffset(26)]
			public byte AmountStackType;

			public bool IsEqual(in ResistanceData other)
			{
				return OverrideType.Equals(other.OverrideType) &&
					OverridePriority.Equals(other.OverridePriority) &&
					AllDamageAmount.Equals(other.AllDamageAmount) &&
					PhysicalDamageAmount.Equals(other.PhysicalDamageAmount) &&
					MagicalDamageAmount.Equals(other.MagicalDamageAmount);
			}
		}


		[FieldOffset(CustomDataOffset)]
		public ResistanceData Resistance;
	}

	[CreateAssetMenu(fileName = "Resistance", menuName = "BehaviourModifier/Resistance")]
	public class ResistanceMod : DurationAndVisualsMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.ResistanceData);

		public class ResistanceContext : DurationAndVisualsContext
		{
			public HealthBehaviour HealthBehaviour;
			public DataOverride<ResistanceAmount> Override;
			public LiteNetworkedData.ResistanceData PreviousData;

			public ResistanceContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				HealthBehaviour = networkContext.Target.GetStateBehaviour<HealthBehaviour>();
			}
		}

		public override void SetDefaults(ref LiteNetworkedData data)
		{
			base.SetDefaults(ref data);
			data.Resistance.OverrideType = (byte)ValueStackingType.Multiply;
			data.Resistance.OverridePriority = (ushort)Priorities.GeneralMult;
			data.Resistance.AllDamageAmount = float.NaN;
			data.Resistance.PhysicalDamageAmount = float.NaN;
			data.Resistance.MagicalDamageAmount = float.NaN;
			data.Resistance.DurationStackType = (byte)ValueStackingType.Replace;
			data.Resistance.AmountStackType = (byte)ValueStackingType.Replace;
		}

		public override bool ModAdded_CombineWith(object rContext, ref LiteNetworkedData current, in LiteNetworkedData next)
		{
			if (ScriptIndex != next.ScriptType) return false;
			if (current.Resistance.StackId != next.Resistance.StackId) return false;
			if (current.Resistance.DurationStackType == (byte)ValueStackingType.DoNotStack) return false;
			if (current.Resistance.AmountStackType == (byte)ValueStackingType.DoNotStack) return false;
			if (current.Resistance.DurationStackType != next.Resistance.DurationStackType) return false;
			if (current.Resistance.AmountStackType != next.Resistance.AmountStackType) return false;

			current.Resistance.Duration = DurationRemaining(current);
			current.Resistance.startTick = SessionManager.Tick;

			current.Resistance.Duration = ValueStack.Stack(current.Resistance.Duration, next.Resistance.Duration, (ValueStackingType)current.Resistance.DurationStackType);

			current.Resistance.AllDamageAmount = ValueStack.Stack(current.Resistance.AllDamageAmount, next.Resistance.AllDamageAmount, (ValueStackingType)current.Resistance.AmountStackType);
			current.Resistance.PhysicalDamageAmount = ValueStack.Stack(current.Resistance.PhysicalDamageAmount, next.Resistance.PhysicalDamageAmount, (ValueStackingType)current.Resistance.AmountStackType);
			current.Resistance.MagicalDamageAmount = ValueStack.Stack(current.Resistance.MagicalDamageAmount, next.Resistance.MagicalDamageAmount, (ValueStackingType)current.Resistance.AmountStackType);

			return true;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			ResistanceContext _context = new ResistanceContext(networkContext);
			context = _context;

			if (!SessionManager.IsServer) return;
			ResetOverride(_context, in data);
		}

		public void ResetOverride(ResistanceContext context, in LiteNetworkedData data)
		{
			if (context.HealthBehaviour == null) return;

			if (context.Override == null)
				context.Override = new DataOverride<ResistanceAmount> { Data = new ResistanceAmount() };

			context.Override.Data.AllMultiplier = data.Resistance.AllDamageAmount;
			context.Override.Data.PhysicalMultiplier = data.Resistance.PhysicalDamageAmount;
			context.Override.Data.MagicalMultiplier = data.Resistance.MagicalDamageAmount;
			context.Override.Priority = data.Resistance.OverridePriority;
			context.Override.StackingType = (ValueStackingType)data.Resistance.OverrideType;

			context.HealthBehaviour.ModifyOverride(context.Override);
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is ResistanceContext ResistanceContext)) return;
			if (ResistanceContext.PreviousData.IsEqual(data.Resistance)) return;
			ResistanceContext.PreviousData = data.Resistance;

			ResetOverride(ResistanceContext, in data);
		}

		public override void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnInvalidatedUpdate(rContext, ref data);

			if (!(rContext is ResistanceContext ResistanceContext)) return;

			if (ResistanceContext.HealthBehaviour != null)
			{
				ResistanceContext.HealthBehaviour.RemoveOverride(ResistanceContext.Override);
			}
		}
	}
}