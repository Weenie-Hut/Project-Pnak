using UnityEngine;
// using Fusion;

namespace Pnak
{
	[CreateAssetMenu(fileName = "Upgrade", menuName = "Pnak/Radial/Upgrade")]
	public class UpgradeRadialOption : CustomCostRadialOption
	{
		public SerializedLiteNetworkedData Data;
		public int ScriptType => Data.scriptType - 1;

		public override Cost SetCost(Interactable interactable = null)
		{
			if (interactable == null)
				return Cost.Zero;
			if (ScriptType < 0 && ScriptType >= LiteNetworkManager.ModScripts.Length)
				throw new System.ArgumentException("ScriptType is not set to a script: " + ScriptType);
			if (!(LiteNetworkManager.ModScripts[ScriptType] is UpgradableMod upgradable))
				throw new System.ArgumentException("ScriptType is not a valid UpgradableMod.");

			if (interactable == null || !interactable.TryGetComponent(out StateBehaviourController controller))
				throw new System.ArgumentException("Interactable is not valid but is still trying to access cost.");

			UnityEngine.Debug.Log("ScriptType: " + ScriptType + " Upgrade Get Cost");
			
			int address = controller.FindModifierAddress(ScriptType, out LiteNetworkedData data);
			if (address != -1)
			{
				return upgradable.GetCost(in data);
			}
			
			return upgradable.GetBaseCost();
		}

		public override bool IsValidTarget(Interactable interactable = null)
		{
			if (!(LiteNetworkManager.ModScripts[ScriptType] is UpgradableMod upgradable))
				throw new System.ArgumentException("ScriptType is not a valid UpgradableMod.");

			if (base.IsValidTarget(interactable) == false)
				return false;

			if (interactable == null || !interactable.TryGetComponent(out StateBehaviourController controller))
				return false;
				
			int address = controller.FindModifierAddress(ScriptType, out LiteNetworkedData data);
			if (address != -1)
			{
				return upgradable.CanUpgrade(in data);
			}

			foreach (var type in upgradable.ValidTargets)
			{
				if (interactable?.GetComponent(type) != null)
					return true;
			}
			return false;
		}

		public override void OnSelect(Interactable interactable = null)
		{
			if (!(LiteNetworkManager.ModScripts[ScriptType] is UpgradableMod upgradable))
				throw new System.ArgumentException("ScriptType is not a valid UpgradableMod.");

			base.OnSelect(interactable);

			StateBehaviourController controller = interactable?.GetComponent<StateBehaviourController>();
			if (controller == null)
				return;

			int address = controller.FindModifierAddress(ScriptType, out LiteNetworkedData data);
			if (address != -1)
			{
				data.Upgradable.UpgradeIndex++;
				LiteNetworkManager.RPC_UpdateModifier(address, data);
				return;
			}

			LiteNetworkManager.RPC_AddModifier(controller.NetworkContext, Data);
		}
	}
}