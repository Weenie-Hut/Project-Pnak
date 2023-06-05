using Fusion;
using UnityEngine;
using System.Collections.Generic;

namespace Pnak
{
	[System.Serializable]
	public struct ShootData
	{
		[Suffix("sec")] public float ReloadTime;
		[Min(0)] public Vector2 FireCountRange;
		[Suffix("°")] public float FireSpreadAngle;
		[Tooltip("The weight of this shoot data when randomly picking a shoot data to use. Temporary shoot data will always be picked before regular shoot data, but will use this random pick weight if multiple temporary data exist.")]
		[Suffix("weight")] public float RandomPickWeight;
		[Required] public StateBehaviourController Spawn;

		public ShootData SetReloadTime(float time)
		{
			ReloadTime = time;
			return this;
		}
		
		public ShootData IncrementReloadTime(float amount)
		{
			ReloadTime += amount;
			return this;
		}

		public ShootData MultiplyReloadTime(float amount)
		{
			ReloadTime *= amount;
			return this;
		}

		public ShootData SetFireCount(Vector2 range)
		{
			FireCountRange = range;
			return this;
		}

		public ShootData IncrementFireCount(float amount)
		{
			FireCountRange += Vector2.one * amount;
			return this;
		}

		public ShootData MultiplyFireCount(float amount)
		{
			FireCountRange *= amount;
			return this;
		}
	}

	public class ShootBehaviour : StateBehaviour
	{
		[Tooltip("Initial firing delays. Whenever reload times are going to be set, the first value in this list will be removed and used if one exits.")]
		public List<ShootData> TemporaryShootDatas = new List<ShootData> {
			new ShootData { ReloadTime = 3.0f, FireCountRange = Vector2.one, FireSpreadAngle = 0.1f }
		};

		public List<ShootData> ShootDatas = new List<ShootData> {
			new ShootData { ReloadTime = 1.5f, FireCountRange = Vector2.one, FireSpreadAngle = 0.1f }
		};

		[Tooltip("If true, the shoot data will be picked at random, after temporary shoot data has been used. Otherwise, the shoot data will be picked in order, cyclicly.")]
		public bool RandomizeShootDataOrder = false;

		#region Fire Behaviour Settings

		public enum FireBehaviourType
		{
			[Tooltip("Behaviour will use player input if exists (even if not shooting), and will use automatic otherwise.")]
			Any = 0,
			[Tooltip("Only player input will be used. Nothing will shoot if player input does not exist.")]
			PlayerInputOnly = 1,
			[Tooltip("Player input will be ignored. Useful if player input is being used somewhere else on the same object. (If input is never being used elsewhere, this is the same as Any)")]
			AutomaticOnly = 2
		}

		[SerializeField]
		[Tooltip("Determines if player input should ever be used, or if it should ever shoot automatically.")]
		private FireBehaviourType FireBehaviour = FireBehaviourType.Any;

		#region Player Settings
		
		[SerializeField, HideIf(nameof(FireBehaviour) + "==2")]
		private NetworkButton PlayerShootButton = NetworkButton.Primary;

		#endregion

		#region Automatic Settings

		[SerializeField, HideIf(nameof(FireBehaviour) + "==1")]
		[Tooltip("When there is no input, the weapon will automatically fire. If true, the weapon will fire as soon as it is ready. If false, the weapon will wait until a target is within the minimum fire angle.")]
		private bool UseMinimumFireAngle = false;
		
		[SerializeField, Attached, HideIf(nameof(UseMinimumFireAngle), true), HideIf(nameof(FireBehaviour) + "==1")]
		private LookAtBehaviour LookAt;

		[SerializeField, Suffix("°"), HideIf(nameof(UseMinimumFireAngle), true), HideIf(nameof(FireBehaviour) + "==1")]
		private float MinimumFireAngle = 25f;

		#endregion

		#endregion

		[SerializeField, Suffix("sec"), Min(0.0166f)]
		private float MinimumReloadTime = 0.07f;

		[SerializeField] private int DefaultDataIndex = 0;
		[HideInInspector] public ShootData DefaultData;

		private int ReloadVisualIndex = -1;

		private int currentShootDataIndex = 0;
		public int CurrentShootDataIndex => currentShootDataIndex;

		public ShootData CurrentShootData => TemporaryShootDatas.Count > 0 ? TemporaryShootDatas[0] : ShootDatas[CurrentShootDataIndex];
		/// <summary>
		/// The time it takes to reload the weapon. Minimum is ~15fps.
		/// </summary>
		public float CurrentReloadTime => Mathf.Max(MinimumReloadTime, CurrentShootData.ReloadTime);
		public StateBehaviourController CurrentSpawn => CurrentShootData.Spawn;

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

		#region Apply Data Modifications

		public void ScaleReloadTimes(float scale, bool includeTemporary = true, bool autoInterpolateTime = true)
		{
			if (scale <= 0.001f) throw new System.ArgumentException("Scale must be a positive number", nameof(scale));

			float currentTime = CurrentReloadTime;

			for (int i = 0; i < ShootDatas.Count; i++)
				ShootDatas[i] = ShootDatas[i].MultiplyReloadTime(scale);

			if (includeTemporary)
			{
				for (int i = 0; i < TemporaryShootDatas.Count; i++)
					TemporaryShootDatas[i] = TemporaryShootDatas[i].MultiplyReloadTime(scale);
			}

			if (autoInterpolateTime)
				InterpolateReloadTime(currentTime);
			else UpdateVisuals();
		}

		public void IncrementReloadTimes(float amount, bool includeTemporary = true, bool autoInterpolateTime = true)
		{
			float currentTime = CurrentReloadTime;

			for (int i = 0; i < ShootDatas.Count; i++)
				ShootDatas[i] = ShootDatas[i].IncrementReloadTime(amount);

			if (includeTemporary)
			{
				for (int i = 0; i < TemporaryShootDatas.Count; i++)
					TemporaryShootDatas[i] = TemporaryShootDatas[i].IncrementReloadTime(amount);
			}

			if (autoInterpolateTime)
				InterpolateReloadTime(currentTime);
			else UpdateVisuals();
		}

		public void IncrementFireCount(float amount, bool includeTemporary = true)
		{
			for (int i = 0; i < ShootDatas.Count; i++)
				ShootDatas[i] = ShootDatas[i].IncrementFireCount(amount);

			if (includeTemporary)
			{
				for (int i = 0; i < TemporaryShootDatas.Count; i++)
					TemporaryShootDatas[i] = TemporaryShootDatas[i].IncrementFireCount(amount);
			}
		}

		public void ScaleFireCount(float scale, bool includeTemporary = true)
		{
			for (int i = 0; i < ShootDatas.Count; i++)
				ShootDatas[i] = ShootDatas[i].MultiplyFireCount(scale);

			if (includeTemporary)
			{
				for (int i = 0; i < TemporaryShootDatas.Count; i++)
					TemporaryShootDatas[i] = TemporaryShootDatas[i].MultiplyFireCount(scale);
			}
		}

		#endregion

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

		public void SetReloadTime()
		{
			EndReloadTick = Runner.Tick + (int)(CurrentReloadTime / Runner.DeltaTime);
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
			ReloadVisualIndex = Controller.FindNetworkMod<ReloadVisualMod>(out int scriptIndex);
			SetReloadTime();
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.Tick < EndReloadTick) return;

			if (Controller.Input.HasValue &&
				(FireBehaviour == FireBehaviourType.Any || FireBehaviour == FireBehaviourType.PlayerInputOnly)
			) {
				if (!Controller.Input.Value.GetButtonDown((byte)PlayerShootButton)) return;
			}
			else if (FireBehaviour == FireBehaviourType.Any || FireBehaviour == FireBehaviourType.AutomaticOnly)
			{
				if (LookAt != null && LookAt.DeltaAngle > MinimumFireAngle) return;
			}
			else return;

			Fire();
		}

		private void Fire()
		{
			// The fractional part of each end of the range if the probability that it is rounded up
			int fireCount = MathUtil.ProbabilityRound(Random.Range(CurrentShootData.FireCountRange.x, CurrentShootData.FireCountRange.y));

			float currentSpread = CurrentShootData.FireSpreadAngle / 2;

			for (int i = 0; i < fireCount; i++)
			{
				float rotationOffset = Random.Range(-currentSpread, currentSpread);

				LiteNetworkManager.QueueNewNetworkObject(CurrentSpawn, new TransformData {
					Position = Controller.TransformData.Position,
					RotationAngle = Controller.TransformData.RotationAngle + rotationOffset
				}, Controller.CopyStateModifiersTo);
			}

			if (TemporaryShootDatas.Count > 0)
			{
				TemporaryShootDatas.RemoveAt(0);

				if (RandomizeShootDataOrder)
				{
					int pick = PickRandomShootIndex(TemporaryShootDatas);
					// Swap the picked index with the first index
					ShootData temp = TemporaryShootDatas[0];
					TemporaryShootDatas[0] = TemporaryShootDatas[pick];
					TemporaryShootDatas[pick] = temp;
				}
			}
			else if (RandomizeShootDataOrder)
			{
				currentShootDataIndex = PickRandomShootIndex(ShootDatas);
			}
			else if (currentShootDataIndex + 1 < ShootDatas.Count)
			{
				currentShootDataIndex++;
			}
			else
			{
				currentShootDataIndex = 0;
			}

			SetReloadTime();
		}

		private static int PickRandomShootIndex(List<ShootData> datas)
		{
			float totalWeight = 0;

			for (int i = 0; i < datas.Count; i++)
			{
				totalWeight += datas[i].RandomPickWeight;
			}

			float pick = Random.Range(0, totalWeight);

			for (int i = 0; i < datas.Count; i++)
			{
				pick -= datas[i].RandomPickWeight;

				if (pick <= 0)
				{
					return i;
				}
			}

			UnityEngine.Debug.LogError("Failed to pick random shoot index");
			return 0;
		}

		private void OnValidate()
		{
			if (DefaultDataIndex < 0 || DefaultDataIndex >= ShootDatas.Count)
			{
				DefaultDataIndex = 0;
			}

			DefaultData = ShootDatas[DefaultDataIndex];
		}
	}
}