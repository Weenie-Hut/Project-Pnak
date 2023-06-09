using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
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
			public ushort Priority;
			[AsEnum(typeof(ValueStackingType))]
			[FieldOffset(10)]
			public byte StackingType;
			[FieldOffset(11)]
			[NaNButton, Min(0f)] public float AllDamageAmount;
			[FieldOffset(15)]
			[NaNButton, Min(0f)] public float PhysicalDamageAmount;
			[FieldOffset(19)]
			[NaNButton, Min(0f)] public float MagicalDamageAmount;

			public bool IsEqual(in ResistanceData other)
			{
				return StackingType.Equals(other.StackingType) &&
					Priority.Equals(other.Priority) &&
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
			data.Resistance.StackingType = (byte)ValueStackingType.Multiply;
			data.Resistance.Priority = (ushort)Priorities.GeneralMult;
			data.Resistance.AllDamageAmount = float.NaN;
			data.Resistance.PhysicalDamageAmount = float.NaN;
			data.Resistance.MagicalDamageAmount = float.NaN;
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
			context.Override.Priority = data.Resistance.Priority;
			context.Override.StackingType = (ValueStackingType)data.Resistance.StackingType;

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