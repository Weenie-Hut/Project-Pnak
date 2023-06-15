using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct DamageData
		{
			/***** Copy of UpgradableData ******/
			/***** Copy of Duration and Visuals ******/
			[Suffix("sec"), Tooltip("The duration of this upgrade. Use 0 or less for permanent upgrades.")]
			[Default(float.PositiveInfinity), Min(0)]
			public float Duration;
			[HideInInspector]
			public int startTick;
			/***********************************/
			[HideInInspector] public ushort UpgradeIndex;
			/***********************************/
		}


		[FieldOffset(CustomDataOffset)]
		public DamageData Damage;
	}

	[CreateAssetMenu(fileName = "Damage", menuName = "BehaviourModifier/Damage")]
	public class DamageMod : GenericUpgradableMod<DataOverride<DamageAmount>>
	{
		public override System.Type DataType => typeof(LiteNetworkedData.DamageData);

		private static readonly System.Type[] _validTargets = { typeof(DamageMunition) };
		public override System.Type[] ValidTargets => _validTargets;

		public class DamageContext : UpgradableContext
		{
			public DamageMunition DamageMunition;

			public DamageContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				DamageMunition = networkContext.Target.GetStateBehaviour<DamageMunition>();
			}
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			DamageContext _context = new DamageContext(networkContext);
			context = _context;
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is DamageContext DamageContext)) return;
			if (DamageContext.UpgradeIndex == data.Upgradable.UpgradeIndex) return;

			if (DamageContext.UpgradeIndex != -1)
			{
				DamageContext.DamageMunition.RemoveOverride(Upgrades[DamageContext.UpgradeIndex].Upgrade);
			}

			DamageContext.UpgradeIndex = data.Upgradable.UpgradeIndex;

			if (DamageContext.UpgradeIndex != -1)
			{
				DamageContext.DamageMunition.AddOverride(Upgrades[DamageContext.UpgradeIndex].Upgrade);
			}
		}

		public override void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnInvalidatedUpdate(rContext, ref data);

			if (!(rContext is DamageContext DamageContext)) return;

			if (DamageContext.UpgradeIndex != -1)
			{
				DamageContext.DamageMunition.RemoveOverride(Upgrades[DamageContext.UpgradeIndex].Upgrade);
			}
		}
	}
}