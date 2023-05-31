using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		public struct StateRunnerData
		{
			public int predictedDestroyTick;
		}

		[FieldOffset(CustomDataOffset)]
		public StateRunnerData StateRunner;
	}

	[CreateAssetMenu(fileName = "StateRunner", menuName = "BehaviourModifier/StateRunner")]
	public class StateRunnerMod : LiteNetworkMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.StateRunnerData);
	
		public class StateRunnerContext
		{
			public LiteNetworkObject NetworkObject;
			public StateBehaviourController controller;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);
			data.StateRunner.predictedDestroyTick = int.MaxValue;
		}

		public override void Initialize(LiteNetworkObject networkObject, in LiteNetworkedData data, out object context)
		{
			if (!SessionManager.Instance.NetworkRunner.IsServer)
			{
				context = networkObject;
				return;
			}

			context = new StateRunnerContext
			{
				NetworkObject = networkObject,
				controller = networkObject.Target.GetComponent<StateBehaviourController>()
			};
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (!(rContext is StateRunnerContext context)) return;

			context.controller.FixedUpdateNetwork(ref data);
		}

		public override void OnRender(object wContext, in LiteNetworkedData data)
		{
			base.OnRender(wContext, data);
			if (!(wContext is StateRunnerContext context)) return;

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;

			if (currentTick >= data.StateRunner.predictedDestroyTick)
				context.NetworkObject.Target.SetActive(false);
			else
				context.NetworkObject.Target.SetActive(true);
		}
	}
}