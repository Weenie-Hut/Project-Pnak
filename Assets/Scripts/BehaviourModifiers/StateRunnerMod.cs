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
			StateRunnerContext runnerContext = new StateRunnerContext
			{
				NetworkObject = networkObject,
				controller = networkObject.Target.GetComponent<StateBehaviourController>()
			};

			if (SessionManager.IsServer)
			{
				runnerContext.controller.Initialize(networkObject);
			}

			context = runnerContext;
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);
			if (!(rContext is StateRunnerContext context))
			{
				UnityEngine.Debug.LogError("rContext is not the correct type: " + rContext.GetType() + " != " + typeof(StateRunnerContext) + ". Object: " + rContext.ToString());
				return;
			}

			context.controller.FixedUpdateNetwork(ref data);
		}

		public override void OnRender(object wContext, in LiteNetworkedData data)
		{
			base.OnRender(wContext, data);
			if (!(wContext is StateRunnerContext context))
			{
				UnityEngine.Debug.LogError("wContext is not the correct type: " + wContext.GetType() + " != " + typeof(StateRunnerContext) + ". Object: " + wContext.ToString());
				return;
			}

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;

			if (currentTick >= data.StateRunner.predictedDestroyTick)
				context.NetworkObject.Target.gameObject.SetActive(false);
			else
				context.NetworkObject.Target.gameObject.SetActive(true);

			context.controller.Render();
		}
	}
}