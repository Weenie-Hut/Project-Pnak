using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct SteamBurstData
		{
			/***** Copy of UpgradableData ******/
			[HideInInspector] public ushort UpgradeIndex;
			/***********************************/
		}


		[FieldOffset(CustomDataOffset)]
		public SteamBurstData SteamBurst;
	}

	[CreateAssetMenu(fileName = "SteamBurst", menuName = "BehaviourModifier/SteamBurst")]
	public class SteamBurstMod : GenericUpgradableMod<SteamBurstMod.SteamBurstData>
	{
		[System.Serializable]
		public class SteamBurstData
		{
			[MinMax(0.07f), Suffix("x")]
			public float ReloadMultiplier = 2f;
			[FixedInterval]
			public int BurstDuration = 3 * 60;
			[FixedInterval]
			public int MinCooldown = 5 * 60;
			[FixedInterval]
			public int MaxCooldown = 15 * 60;
			
			[Suffix("odds by dist"), Tooltip("Every tick, the ")]
			public float SkipOddsByEnemyDistance = 0.01f;


			[System.NonSerialized] private DataOverride<ShootData> _override = null;
			public DataOverride<ShootData> Override => _override ?? (_override = ConvertToOverride());

			private DataOverride<ShootData> ConvertToOverride()
			{
				DataOverride<ShootData> result = new DataOverride<ShootData>();
				result.Priority = (ushort)Priorities.GeneralMult;
				result.StackingType = ValueStackingType.Multiply;
				result.Data = ShootData.CreateNulled();
				result.Data.ReloadTime = 1f / ReloadMultiplier;
				UnityEngine.Debug.Log($"Reload time: {result.Data.ReloadTime}");
				return result;
			}
		}

		public override System.Type DataType => typeof(LiteNetworkedData.SteamBurstData);

		private static readonly System.Type[] _validTargets = { typeof(ShootBehaviour) };
		public override System.Type[] ValidTargets => _validTargets;

		public class SteamBurstContext : UpgradableContext
		{
			public ShootBehaviour ShootBehaviour;

			public int ticksLeft;
			public bool isBursting;
			public int pickTicks;

			public SteamBurstContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				ShootBehaviour = networkContext.Target.GetStateBehaviour<ShootBehaviour>();
			}
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			SteamBurstContext _context = new SteamBurstContext(networkContext);
			context = _context;

			_context.UpgradeIndex = data.SteamBurst.UpgradeIndex;
			SetCooldown(_context);
		}

		private void SetCooldown(SteamBurstContext context)
		{
			context.ticksLeft = Random.Range(Upgrades[context.UpgradeIndex].Upgrade.MinCooldown, Upgrades[context.UpgradeIndex].Upgrade.MaxCooldown);
			context.isBursting = false;
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is SteamBurstContext SteamBurstContext)) return;

			if (SteamBurstContext.UpgradeIndex != data.Upgradable.UpgradeIndex)
			{
				if (SteamBurstContext.UpgradeIndex != -1 && SteamBurstContext.isBursting)
				{
					SteamBurstContext.ShootBehaviour.RemoveOverride(Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.Override);
				}

				SteamBurstContext.UpgradeIndex = data.Upgradable.UpgradeIndex;

				if (SteamBurstContext.UpgradeIndex != -1 && SteamBurstContext.isBursting)
				{
					SteamBurstContext.ShootBehaviour.AddOverride(Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.Override);
				}
			}

			if (SteamBurstContext.UpgradeIndex == -1) return;

			SteamBurstContext.ticksLeft--;

			if (SteamBurstContext.ticksLeft > 0 && !SteamBurstContext.isBursting && SteamBurstContext.pickTicks-- <= 0)
			{
				SteamBurstContext.pickTicks = 60;
				float toBeat = Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.SkipOddsByEnemyDistance *
					Mathf.Pow(Enemy.NormalizedFurthestEnemyDistance(), 2);
				if (Random.value < toBeat)
				{
					SteamBurstContext.ticksLeft = 0;
				}
			}

			if (SteamBurstContext.ticksLeft <= 0)
			{
				if (SteamBurstContext.isBursting)
				{
					SteamBurstContext.ShootBehaviour.RemoveOverride(Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.Override);
					SetCooldown(SteamBurstContext);
					// UnityEngine.Debug.Log("Cooldown: " + SteamBurstContext.ticksLeft * SessionManager.DeltaTime);
				}
				else
				{
					SteamBurstContext.ShootBehaviour.AddOverride(Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.Override);
					SteamBurstContext.isBursting = true;
					SteamBurstContext.ticksLeft = Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.BurstDuration;
					// UnityEngine.Debug.Log("Burst: " + SteamBurstContext.ticksLeft * SessionManager.DeltaTime);
				}
			}
		}

		public override void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnInvalidatedUpdate(rContext, ref data);

			if (!(rContext is SteamBurstContext SteamBurstContext)) return;

			if (SteamBurstContext.UpgradeIndex != -1)
			{
				SteamBurstContext.ShootBehaviour.RemoveOverride(Upgrades[SteamBurstContext.UpgradeIndex].Upgrade.Override);
			}
		}
	}
}