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

// 	public abstract class StateModifier
// 	{
// 		private int startTick;
// 		protected StackableFloat Duration;

// 		public StateBehaviourController Controller { get; private set; }

// 		public abstract StateModifier CopyFor(StateBehaviourController controller);

// 		public virtual bool TryStackWith(StateModifier other) => false;

// 		protected void SetDuration(ValueStackSettings<float> duration)
// 		{
// 			if (Duration == null)
// 				Duration = StackableFloat.Create(duration);
// 			else
// 				Duration.Value = duration;

// 			ResetStartTick();
// 		}

// 		protected bool ResetStartTick(bool changed = true)
// 		{
// 			if (changed)
// 			{
// 				startTick = SessionManager.Instance.NetworkRunner.Tick;
// 				if (Duration <= 0)
// 					Duration.Value = float.PositiveInfinity;
// 			}

// 			return changed;
// 		}

// 		protected float GetTimeLeft()
// 		{
// 			if (Duration <= 0) return float.PositiveInfinity;

// 			int currentTick = SessionManager.Instance.NetworkRunner.Tick;
// 			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;

// 			return Duration - (currentTick - startTick) * tickRate;
// 		}

// 		public virtual void Added(StateBehaviourController controller)
// 		{
// 			Controller = controller;
// 		}

// 		public virtual void FixedUpdateNetwork()
// 		{
// 			if (GetTimeLeft() <= 0)
// 			{
// 				Controller.RemoveStateModifier(this);
// 			}
// 		}

// 		public virtual void Removed()
// 		{
// 		}
// 	}
}