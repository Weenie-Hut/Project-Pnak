using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct ShootUpgradeData
		{
			/***** Copy of UpgradableData ******/
			[HideInInspector] public ushort UpgradeIndex;
			/***********************************/
		}


		[FieldOffset(CustomDataOffset)]
		public ShootUpgradeData ShootUpgrade;
	}

	[CreateAssetMenu(fileName = "ShootUpgrade", menuName = "BehaviourModifier/ShootUpgrade")]
	public class ShootUpgradeMod : GenericUpgradableMod<DataOverride<ShootData>>
	{
		public override System.Type DataType => typeof(LiteNetworkedData.ShootUpgradeData);

		private static readonly System.Type[] _validTargets = { typeof(ShootBehaviour) };
		public override System.Type[] ValidTargets => _validTargets;

		public class ShootUpgradeContext : UpgradableContext
		{
			public ShootBehaviour ShootBehaviour;

			public ShootUpgradeContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				ShootBehaviour = networkContext.Target.GetStateBehaviour<ShootBehaviour>();
			}
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			ShootUpgradeContext _context = new ShootUpgradeContext(networkContext);
			context = _context;
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is ShootUpgradeContext ShootUpgradeContext)) return;
			if (ShootUpgradeContext.UpgradeIndex == data.Upgradable.UpgradeIndex) return;

			if (ShootUpgradeContext.UpgradeIndex != -1)
			{
				ShootUpgradeContext.ShootBehaviour.RemoveOverride(Upgrades[ShootUpgradeContext.UpgradeIndex].Upgrade);
			}

			ShootUpgradeContext.UpgradeIndex = data.Upgradable.UpgradeIndex;

			if (ShootUpgradeContext.UpgradeIndex != -1)
			{
				ShootUpgradeContext.ShootBehaviour.AddOverride(Upgrades[ShootUpgradeContext.UpgradeIndex].Upgrade);
			}
		}

		public override void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnInvalidatedUpdate(rContext, ref data);

			if (!(rContext is ShootUpgradeContext ShootUpgradeContext)) return;

			if (ShootUpgradeContext.UpgradeIndex != -1)
			{
				ShootUpgradeContext.ShootBehaviour.RemoveOverride(Upgrades[ShootUpgradeContext.UpgradeIndex].Upgrade);
			}
		}
	}
}