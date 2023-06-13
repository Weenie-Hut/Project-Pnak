using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkObject
	{
		public readonly int Index;
		public readonly List<int> Modifiers;
		public StateBehaviourController Target { get; private set; }
		public Fusion.PlayerRef InputAuthority { get; private set; }
		public int PrefabIndex { get; private set; }
		public bool QueuedForDestruction { get; private set; }

		public bool IsValid {
			get {
#if DEBUG
				if (!SessionManager.IsServer)
				{
					UnityEngine.Debug.Assert((PrefabIndex == -1 && Target == null) || (PrefabIndex != -1 && Target != null), $"Invalid LiteNetworkObject state! {Format()}");
				}
#endif
				return Target != null && Modifiers.Count > 0;
			}
		}

		public bool IsReserved => PrefabIndex != -1;

		public LiteNetworkObject(int index)
		{
			Modifiers = new List<int>();
			Target = null;
			PrefabIndex = -1;
			Index = index;
			QueuedForDestruction = false;
		}

		public override string ToString() => Format();

		public string Format()
		{
			return $"{{\n\tIsReserved: {IsValid}" +
				"\n\tModifiers:\n\t\t" + Modifiers.Select(addr => $"Addr {addr} = {LiteNetworkManager.GetModifierData(addr)}").Format("\n\t\t", "", "") +
				$"\n\tTarget: {Target}" +
				$"\n\tPrefabIndex: {PrefabIndex}\n}}";
		}

		public StateBehaviourController LOCAL_Free()
		{
#if DEBUG
			if (SessionManager.IsServer)
			{
				UnityEngine.Debug.Assert(IsReserved, $"Attempted to free a reserved object! {Format()}");
				UnityEngine.Debug.Assert(InputAuthority == Fusion.PlayerRef.None, $"Attempted to free an object with input authority! {Format()}");
			}
			UnityEngine.Debug.Assert(Modifiers.Count == 0, $"Attempted to free an object with modifiers! {Format()}");
			UnityEngine.Debug.Assert(Target != null, $"Attempted to free an object with no target! {Format()}");
#endif

			StateBehaviourController target = Target;
			PrefabIndex = -1;
			Target = null;
			QueuedForDestruction = false;
			return target;
		}

		public void STATE_ReserveAs(int prefabIndex)
		{
#if DEBUG
			UnityEngine.Debug.Assert(SessionManager.IsServer, $"Attempted to reserve an object on a client! {Format()}");
			UnityEngine.Debug.Assert(!IsReserved, $"Attempted to reserve an already reserved object! {Format()}");
			UnityEngine.Debug.Assert(Target == null, $"Attempted to reserve an object with a target! {Format()}");
			UnityEngine.Debug.Assert(Modifiers.Count == 0, $"Attempted to reserve an object with modifiers! {Format()}");
			UnityEngine.Debug.Assert(QueuedForDestruction == false, $"Attempted to reserve an object queued for destruction! {Format()}");
			UnityEngine.Debug.Assert(InputAuthority == Fusion.PlayerRef.None, $"Attempted to reserve an object with input authority! {Format()}");
#endif

			PrefabIndex = prefabIndex;
		}

		public void LOCAL_Populate(StateBehaviourController target)
		{
#if DEBUG
			if (SessionManager.IsServer)
			{
				UnityEngine.Debug.Assert(IsReserved, $"Attempted to populate a non reserved object! {Format()}");
				UnityEngine.Debug.Assert(target.PrefabIndex == PrefabIndex, $"Attempted to populate an object with a target of the wrong type! {Format()}");
			}

			UnityEngine.Debug.Assert(Target == null, $"Attempted to populate an object with a target! {Format()}");
			UnityEngine.Debug.Assert(Modifiers.Count == 0, $"Attempted to populate an object with modifiers! {Format()}");
			UnityEngine.Debug.Assert(target != null, $"Attempted to populate an object with a null target! {Format()}");
			UnityEngine.Debug.Assert(QueuedForDestruction == false, $"Attempted to reserve an object queued for destruction! {Format()}");
#endif

			Target = target;
			PrefabIndex = target.PrefabIndex; // Sets for clients (server must reserve as index before populating)
		}

		public void STATE_QueueForDestruction()
		{
#if DEBUG
			UnityEngine.Debug.Assert(SessionManager.IsServer, $"Attempted to queue an object for destruction on a client! {Format()}");
			UnityEngine.Debug.Assert(IsReserved, $"Attempted to queue a non reserved object for destruction! {Format()}");
			UnityEngine.Debug.Assert(Target != null, $"Attempted to queue an object with no target for destruction! {Format()}");
			UnityEngine.Debug.Assert(Modifiers.Count != 0, $"Attempted to queue an object with no modifiers for destruction! {Format()}");
#endif

			QueuedForDestruction = true;
		}

		public void SetInputAuthority(Fusion.PlayerRef playerRef)
		{
#if DEBUG
			if (SessionManager.IsServer)
			{
				UnityEngine.Debug.Assert(IsReserved, $"Attempted to set input authority on a non reserved object! {Format()}");
			}
#endif
			InputAuthority = playerRef;
		}
	}
}