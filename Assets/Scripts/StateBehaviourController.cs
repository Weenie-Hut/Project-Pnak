using UnityEngine;
using System;
using System.Collections;

namespace Pnak
{
	[DisallowMultipleComponent]
	public class StateBehaviourController : MonoBehaviour
	{
		public SerializedLiteNetworkedData[] DefaultMods;

		private LiteNetworkedData[] data;
		public LiteNetworkedData[] Data
		{
			get {
				if (data == null)
				{
					bool hasStateBehaviour = stateBehaviours.Length != 0;
					data = new LiteNetworkedData[DefaultMods.Length + (hasStateBehaviour ? 1 : 0)];
					for (int i = 0; i < DefaultMods.Length; i++)
						data[i] = DefaultMods[i].ToLiteNetworkedData();

					if (hasStateBehaviour)
					{
						StateRunnerMod.SetDefaults(ref data[DefaultMods.Length]);
					}
				}
				return data;
			}
		}

		[SerializeField, ReadOnly] private StateBehaviour[] stateBehaviours;

		public bool QueuedForDestroy { get; private set; }
		public void QueueForDestroy() => QueuedForDestroy = true;

		private delegate void UpdateNetworkData(ref LiteNetworkedData data);
		private event UpdateNetworkData updateNetworkData = null;
		public void FixedUpdateNetwork(ref LiteNetworkedData data)
		{
			foreach (StateBehaviour state in stateBehaviours)
			{
				state.FixedUpdateNetwork();
			}

			if (updateNetworkData != null)
			{
				updateNetworkData(ref data);
				updateNetworkData = null;
			}
		}

		private int predictedDestroyTick = -1;
		public void SetPredictedDestroyTick(int tick)
		{
			predictedDestroyTick = tick;
			updateNetworkData += _SetPredictedDestroyTick;
		}

		private void _SetPredictedDestroyTick(ref LiteNetworkedData data)
		{
			data.StateRunner.predictedDestroyTick = predictedDestroyTick;
			predictedDestroyTick = -1;
		}

		[SerializeField, ReadOnly] private int prefabIndex = -1;
		public int PrefabIndex => prefabIndex;
		[SerializeField, ReadOnly] private StateRunnerMod StateRunnerMod;

		public virtual int DefaultModsCount => Data.Length;
		public virtual LiteNetworkedData[] GetDefaultMods(ref TransformData transform)
		{
			LiteNetworkedData[] mods = new LiteNetworkedData[Data.Length];
			System.Array.Copy(Data, mods, Data.Length);

			foreach (StateBehaviour state in stateBehaviours)
			{
				state.OnDataCreated(mods, ref transform);
			}

			return mods;
		}

#if UNITY_EDITOR
		internal void SetHiddenSerializedFields(int prefabIndex, StateRunnerMod stateRunnerMod)
		{
			this.prefabIndex = prefabIndex;
			StateRunnerMod = stateRunnerMod;
			stateBehaviours = GetComponents<StateBehaviour>();
		}

		private void OnValidate()
		{
			stateBehaviours = GetComponents<StateBehaviour>();
		}
#endif
	}
}