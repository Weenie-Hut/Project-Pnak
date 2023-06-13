using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public enum DoTStackIds
	{
		RedPepperFlamingBullets = 2520,
	}

	public partial struct LiteNetworkedData
	{
		[System.Serializable, StructLayout(LayoutKind.Explicit)]
		public struct DoTData
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
			[HideInInspector]
			[FieldOffset(8)]
			public ushort intervalStartTick;
			[Suffix("x"), Min(0.01f), Tooltip("The multiplier to apply to the default DoT damage.")]
			[FieldOffset(10)]
			public float Scale;
			[Tooltip("The stack ID of this DoT. If a new DoT is applied to a target with the same stack ID, the old DoT will be replaced with the new one."), EnumNameSuffix(typeof(DoTStackIds))]
			[FieldOffset(14)]
			public short StackId;
		}


		[FieldOffset(CustomDataOffset)]
		public DoTData DoT;
	}

	[CreateAssetMenu(fileName = "DoT", menuName = "BehaviourModifier/DoT")]
	public class DoTMod : DurationAndVisualsMod
	{
		[SerializeField]
		private DataSelector<DamageAmount> DamageDataSelector = new DataSelector<DamageAmount>();

		[Tooltip("The time between each time damage is done. If less than runner.DeltaTime, damage will be applied every frame to every overlapping target.")]
		[Suffix("sec"), MinMax(min: 0.0f)]
		public float IntervalBetweenHits = 0.33f;

		[AsLabel(LabelType.Italic | LabelType.Right, "Damage is in d/sec and dealt every {0} network tick(s)")]
		[HideIf(nameof(intervalInTicks), 1)]
		[SerializeField]
		private int intervalInTicks; // Used for any interval

		public ValueStackingType DurationStackingType = ValueStackingType.DoNotStack;
		[HideIf(nameof(DurationStackingType), 0), Default(1)]
		public ValueStackingType DamageScaleStackingType = ValueStackingType.DoNotStack;
		
		public enum DoTStackBehaviour
		{
			[Tooltip("Next damage time will not change when stacking.")]
			KeepInterval,
			[Tooltip("Next damage time is set to happen after a full new interval.")]
			ResetInterval,
			[Tooltip("Next damage time happens immediately.")]
			DamageImmediately,
		}
		[HideIf(nameof(DurationStackingType), 0), HideIf(nameof(intervalInTicks), 1)]
		public DoTStackBehaviour BehaviourOnStack = DoTStackBehaviour.DamageImmediately;

		protected override void OnValidate()
		{
			intervalInTicks = Mathf.RoundToInt(IntervalBetweenHits / SessionManager.DeltaTime);
			if (intervalInTicks <= 1) intervalInTicks = 1;
		}

		public override System.Type DataType => typeof(LiteNetworkedData.DoTData);

		public class DoTContext : DurationAndVisualsContext
		{
			public HealthBehaviour HealthBehaviour;
			public DataSelector<DamageAmount> DamageSelector;

			public DoTContext(LiteNetworkObject networkContext, DataSelector<DamageAmount> selector) : base(networkContext)
			{
				HealthBehaviour = networkContext.Target.GetStateBehaviour<HealthBehaviour>();
				DamageSelector = selector;
			}
		}

		public override bool ModAdded_CombineWith(object rContext, ref LiteNetworkedData current, in LiteNetworkedData next)
		{
			if (ScriptIndex != next.ScriptType) return false;
			if (current.DoT.StackId != next.DoT.StackId) return false;
			if (DurationStackingType == ValueStackingType.DoNotStack) return false;
			if (DamageScaleStackingType == ValueStackingType.DoNotStack) return false;

			current.DoT.Duration = ValueStack.Stack(current.DoT.Duration, next.DoT.Duration, DurationStackingType);
			current.DoT.Scale = ValueStack.Stack(current.DoT.Scale, next.DoT.Scale, DamageScaleStackingType);
			if (BehaviourOnStack == DoTStackBehaviour.ResetInterval)
				current.DoT.intervalStartTick = (ushort)(SessionManager.Tick);
			else if (BehaviourOnStack == DoTStackBehaviour.DamageImmediately)
				current.DoT.intervalStartTick = 0;
			return true;
		}

		public override void SetDefaults(ref LiteNetworkedData data)
		{
			base.SetDefaults(ref data);

			data.DoT.Scale = 1;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			DoTContext _context = new DoTContext(networkContext, DamageDataSelector.Copy());
			context = _context;
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (data.DoT.intervalStartTick + intervalInTicks > SessionManager.Tick) return;

			if (!(rContext is DoTContext DoTContext)) return;
			
			DataSelector<DamageAmount> selector = DoTContext.DamageSelector;
			if (selector.MoveToNext())
			{
				// UnityEngine.Debug.Log("DoT: Current Data has changed => " + selector.CurrentData + " => " + data.DoT.Scale);
				selector.CurrentData.Scale(
					SessionManager.Instance.NetworkRunner.DeltaTime *
					(intervalInTicks) *
					data.DoT.Scale
				);
				// UnityEngine.Debug.Log("DoT: Current Data has changed => " + selector.CurrentData);
			}

			DoTContext.HealthBehaviour.AddDamage(selector.CurrentData);

			if (intervalInTicks > 0)
			{
				data.DoT.intervalStartTick = (ushort)(SessionManager.Tick);
			}
		}
	}
}