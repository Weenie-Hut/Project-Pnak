using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public class OverrideContainer<T> : MonoBehaviour, Overridable<T> where T : Stackable<T>, Copyable<T>
	{
		[Attached, SerializeField] private StateBehaviourController _controller;
		public StateBehaviourController Controller => _controller;

		private SortedSet<DataOverride<T>> DataOverrides = new SortedSet<DataOverride<T>>(new DuplicateKeyComparer<T>());

		public void AddOverride(DataOverride<T> data)
		{
			DataOverrides.Add(data);
		}

		public void ModifyOverride(DataOverride<T> data)
		{
			// Nothing needs to happen as the data does not change any state.
			// DataOverrides.Remove(data);
			// DataOverrides.Add(data);
		}

		public void RemoveOverride(DataOverride<T> data)
		{
			DataOverrides.Remove(data);
		}

		public void QueueSpawnWithOverrides(StateBehaviourController prefab)
		{
			LiteNetworkManager.QueueNewNetworkObject(prefab, Controller.HasTransform ? Controller.TransformCache.Value : new TransformData {
				Position = transform.position,
				RotationAngle = transform.localEulerAngles.z,
				Scale = transform.localScale
			}, CopyOverrides);
		}

		private void CopyOverrides(LiteNetworkObject networkObject)
		{
			foreach(Overridable<T> overridable in networkObject.Target.GetComponents<Overridable<T>>())
				foreach(DataOverride<T> data in DataOverrides)
					overridable.AddOverride(data);
		}
	}
}