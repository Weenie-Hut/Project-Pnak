using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Pnak
{
	public enum InputBehaviourType
	{
		[Tooltip("Behaviour will use player input if exists, and will use automatic otherwise.")]
		Any = 0,
		[Tooltip("Only player input will be used. Nothing will shoot if player input does not exist.")]
		PlayerInputOnly = 1,
		[Tooltip("Player input will be ignored. Useful if player input is being used somewhere else on the same object. (If input is never being used elsewhere, this is the same as Any)")]
		AutomaticOnly = 2
	}

	public enum Priorities
	{
		GeneralAddBefore = 100,
		GeneralMult = 200,
		GeneralAddAfter = 300,
	}

	
	[System.Serializable]
	public class DataOverride<T>
	{
		[EnumNameSuffix(typeof(Priorities))]
		public ushort Priority = (ushort)Priorities.GeneralMult;
		public ValueStackingType StackingType = ValueStackingType.Add;
		public T Data = default;
	}

	public class ShootBehaviour : StateBehaviour
	{
		[SerializeField, Tooltip("The data to use when shooting.")]
		private DataSelectorWithOverrides<ShootData> ShootDataSelector = new DataSelectorWithOverrides<ShootData>();

		#region Fire Behaviour Settings

		[SerializeField]
		[Tooltip("Determines if player input should ever be used, or if it should ever shoot automatically.")]
		private InputBehaviourType FireBehaviour = InputBehaviourType.Any;

		#region Player Settings
		
		[SerializeField, HideIf(nameof(FireBehaviour), 2)]
		private NetworkButton PlayerShootButton = NetworkButton.Primary;

		#endregion

		#region Automatic Settings

		[SerializeField, HideIf(nameof(FireBehaviour), 1)]
		[Tooltip("When there is no input, the weapon will automatically fire. If true, the weapon will fire as soon as it is ready. If false, the weapon will wait until a target is within the minimum fire angle.")]
		private bool UseMinimumFireAngle = false;
		
		[SerializeField, Attached, ShowIf(nameof(UseMinimumFireAngle)), HideIf(nameof(FireBehaviour), 1)]
		private LookAtBehaviour LookAt;

		[SerializeField, Suffix("°"), ShowIf(nameof(UseMinimumFireAngle)), HideIf(nameof(FireBehaviour), 1)]
		private float MinimumFireAngle = 25f;

		#endregion

		#endregion

		[SerializeField, Suffix("sec"), Min(0.0166f)]
		private float MinimumReloadTime = 0.07f;

		private int ReloadVisualIndex = -1;

		/// <summary>
		/// The time it takes to reload the weapon. Minimum is ~15fps.
		/// </summary>
		public float CurrentReloadTime => Mathf.Max(MinimumReloadTime, ShootDataSelector.CurrentData.ReloadTime);

		private int endReloadTick = -1;
		private int EndReloadTick
		{
			get => endReloadTick;
			set {
				if (value == endReloadTick) return;
				endReloadTick = value;
				UpdateVisuals();
			}
		}

		protected override void Awake()
		{
			base.Awake();

			if (UseMinimumFireAngle && LookAt == null)
			{
				UnityEngine.Debug.LogWarning($"ShootBehaviour {name} ({GetType().Name}) has UseMinimumFireAngle set to true but no LookAtBehaviour set");
			}
			else if (!UseMinimumFireAngle)
			{
				LookAt = null;
			}
		}

		public void AddOverride(DataOverride<ShootData> dataOverride)
		{
			float previousTime = CurrentReloadTime;
			ShootDataSelector.AddOverride(dataOverride);
			InterpolateReloadTime(previousTime);
		}

		public void RemoveOverride(DataOverride<ShootData> dataOverride)
		{
			float previousTime = CurrentReloadTime;
			ShootDataSelector.RemoveOverride(dataOverride);
			InterpolateReloadTime(previousTime);
		}

		public void ModifyOverride(DataOverride<ShootData> dataOverride)
		{
			float previousTime = CurrentReloadTime;
			ShootDataSelector.ModifyOverride(dataOverride);
			InterpolateReloadTime(previousTime);
		}

		public void InterpolateReloadTime(float previousTime)
		{
			float startReloadTick = EndReloadTick - (previousTime / Runner.DeltaTime);
			float progress = (Runner.Tick - startReloadTick) / (float)(EndReloadTick - startReloadTick);

			EndReloadTick = Runner.Tick + (int)((CurrentReloadTime * (1f - progress)) / Runner.DeltaTime);
		}

		public void UpdateVisuals()
		{
			if (ReloadVisualIndex >= 0)
			{
				LiteNetworkedData data = LiteNetworkManager.GetModifierData(ReloadVisualIndex);
				data.ReloadVisual.endTick = EndReloadTick;
				data.ReloadVisual.seconds = CurrentReloadTime;
				LiteNetworkManager.SetModifierData(ReloadVisualIndex, data);
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			int selfTypeIndex = Controller.GetBehaviourTypeIndex(this);
			ReloadVisualIndex = Controller.FindNetworkMod<ReloadVisualMod>(selfTypeIndex, out int scriptIndex);
			
			ShootDataSelector.MoveToNext();
			EndReloadTick = Runner.Tick + (int)(CurrentReloadTime / Runner.DeltaTime);
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.Tick < EndReloadTick) return;

			if (Controller.Input.HasValue &&
				(FireBehaviour == InputBehaviourType.Any || FireBehaviour == InputBehaviourType.PlayerInputOnly)
			) {
				if (!Controller.Input.Value.GetButtonDown((byte)PlayerShootButton)) return;
			}
			else if (FireBehaviour == InputBehaviourType.Any || FireBehaviour == InputBehaviourType.AutomaticOnly)
			{
				if (LookAt != null && LookAt.DeltaAngle > MinimumFireAngle) return;
			}
			else return;

			Fire();
		}

		private void Fire()
		{
			ShootData shootData = ShootDataSelector.CurrentData;
			// The fractional part of each end of the range if the probability that it is rounded up
			int fireCount = MathUtil.ProbabilityRound(Random.Range(shootData.FireCountRange.x.NaNTo0(), shootData.FireCountRange.y.NaNTo0()));

			float currentSpread = shootData.FireSpreadAngle.NaNTo0() / 2;

			for (int i = 0; i < fireCount; i++)
			{
				float rotationOffset = Random.Range(-currentSpread, currentSpread);

				if (shootData.Spawn != null)
				{
					LiteNetworkManager.QueueNewNetworkObject(shootData.Spawn, new TransformData {
						Position = Controller.TransformData.Position,
						RotationAngle = Controller.TransformData.RotationAngle + rotationOffset
					}, n => CopyMunitionAndDamageMods(n, shootData));
				}
			}

			ShootDataSelector.MoveToNext();
			EndReloadTick = Runner.Tick + (int)(CurrentReloadTime / Runner.DeltaTime);
		}

		private static void CopyMunitionAndDamageMods(LiteNetworkObject networkObject, ShootData current)
		{
			foreach (var mod in current.MunitionMods)
			{
				LiteNetworkManager.QueueAddModifier(networkObject, mod);
			}

			DamageMunition damageMunition = networkObject.Target.GetStateBehaviour<DamageMunition>();
			if (damageMunition != null)
			{
				foreach (DataOverride<DamageAmount> overrides in current.DamageMods)
				{
					damageMunition.AddOverride(overrides);
				}
			}
		}
	}
}