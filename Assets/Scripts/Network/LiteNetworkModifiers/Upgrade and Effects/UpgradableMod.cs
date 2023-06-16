using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{

	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct UpgradableData
		{
			[HideInInspector] public ushort UpgradeIndex;
		}


		[FieldOffset(CustomDataOffset)]
		public UpgradableData Upgradable;
	}

	[System.Serializable]
	public struct UpgradeData<T>
	{
		public T Upgrade;
		public Cost cost;
	}

	public abstract class GenericUpgradableMod<T> : UpgradableMod
	{
		public List<UpgradeData<T>> Upgrades = new List<UpgradeData<T>>();

		public override Cost GetBaseCost() => Upgrades[0].cost;
		public override Cost GetCost(in LiteNetworkedData data)
		{
			// UnityEngine.Debug.Log("UpgradeIndex: " + data.Upgradable.UpgradeIndex);
			return Upgrades[data.Upgradable.UpgradeIndex + 1].cost;
		}
		public override bool CanUpgrade(in LiteNetworkedData data) => data.Upgradable.UpgradeIndex < Upgrades.Count - 1;
	}

	public abstract class UpgradableMod : VisualsMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.UpgradableData);

		public abstract System.Type[] ValidTargets { get; }

		public class UpgradableContext : VisualsContext
		{
			public int UpgradeIndex;

			public UpgradableContext(LiteNetworkObject networkContext) : base(networkContext)
			{
				UpgradeIndex = -1;
			}
		}

		public abstract Cost GetBaseCost();
		public abstract Cost GetCost(in LiteNetworkedData data);
		public abstract bool CanUpgrade(in LiteNetworkedData data);

		public override void SetDefaults(ref LiteNetworkedData data)
		{
			base.SetDefaults(ref data);
			data.Upgradable.UpgradeIndex = 0;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
			=> context = new UpgradableContext(networkContext);
	}

	
}