using UnityEngine;
using Fusion;

namespace Pnak
{
	public abstract class StateModifierSO : CostRadialOption
	{
#if UNITY_EDITOR
		[Button(nameof(AddToScripts), "Add", nameof(typeIndex) + "!=-1")]
		[Button(nameof(RemoveFromScripts), "Rem", nameof(typeIndex) + "==-1")]
#endif
		[SerializeField, ReadOnly]
		private int typeIndex = -1;

#if UNITY_EDITOR
		public void EditorSetTypeIndex(int index) => typeIndex = index;
		public void AddToScripts() => LiteNetworkModScripts.AddStateModifier(this);
		public void RemoveFromScripts()
		{
			LiteNetworkModScripts.RemoveStateModifier(this);
			typeIndex = -1;
		}

		protected override void OnValidate()
		{
			typeIndex = LiteNetworkModScripts.ValidateStateModifierIndex(this);

			base.OnValidate();
		}
#endif
		public int TypeIndex => typeIndex;

		public abstract StateModifier CreateModifier();

		public override bool IsValidTarget(Interactable interactable = null)
		{
			return base.IsValidTarget(interactable) && interactable?.GetComponent<StateBehaviourController>() != null;
		}

		public override void OnSelect(Interactable interactable = null)
		{
			base.OnSelect(interactable);

			StateBehaviourController controller = interactable?.GetComponent<StateBehaviourController>();
			if (controller == null)
				return;

			LiteNetworkManager.RPC_AddStateMod(controller.TargetNetworkIndex, (ushort)typeIndex);
		}
	}

	public abstract class StateModifier
	{
		private int startTick;
		protected StackableFloat Duration;

		public StateBehaviourController Controller { get; private set; }

		public abstract StateModifier CopyFor(StateBehaviourController controller);

		public virtual bool TryStackWith(StateModifier other) => false;

		protected void SetDuration(ValueStackSettings<float> duration)
		{
			if (Duration == null)
				Duration = StackableFloat.Create(duration);
			else
				Duration.Value = duration;

			ResetStartTick();
		}

		protected bool ResetStartTick(bool changed = true)
		{
			if (changed)
			{
				startTick = SessionManager.Instance.NetworkRunner.Tick;
				if (Duration <= 0)
					Duration.Value = float.PositiveInfinity;
			}

			return changed;
		}

		protected float GetTimeLeft()
		{
			if (Duration <= 0) return float.PositiveInfinity;

			int currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;

			return Duration - (currentTick - startTick) * tickRate;
		}

		public virtual void Added(StateBehaviourController controller)
		{
			Controller = controller;
		}

		public virtual void FixedUpdateNetwork()
		{
			if (GetTimeLeft() <= 0)
			{
				Controller.RemoveStateModifier(this);
			}
		}

		public virtual void Removed()
		{
		}
	}
}