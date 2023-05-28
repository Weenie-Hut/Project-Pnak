using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class NetworkObjectContext
	{
		public bool IsReserved;
		public List<int> Modifiers;
		public GameObject Target;
		public int PrefabIndex;

		public NetworkObjectContext()
		{
			IsReserved = false;
			Modifiers = new List<int>();
			Target = null;
			PrefabIndex = -1;
		}
	}

	public class BehaviourModifierManager : NetworkBehaviour
	{
		public static BehaviourModifierManager Instance { get; private set; }

		public const int ModifierCapacity = 96;

		[SerializeField] private BehaviourModifier[] ModifierScripts;
		[SerializeField] private GameObject[] PseudoNetworkPrefabs;

		[Networked, Capacity(ModifierCapacity)]
		private NetworkArray<BehaviourModifierData> ModifiersData { get; }
		/// <summary>
		/// The highest index in ModifiersData that is currently in use.
		/// Used as a hint to avoid iterating over the entire array (performance).
		/// </summary>
		[Networked] private int modifiersDataUsingCapacity { get; set; }
		// TODO: This is to make sure that if an index is disabled and enabled with different data, we can clear the data first.
		private BehaviourModifierData[] _modifiersDataCopy;

		private ReferencePool<GameObject>[] _pseudoNetworkPrefabPools;

		private object[] _modifierContexts;

		private List<NetworkObjectContext> networkTargets;

		private Queue<int> _deletingTargets;

		public override void Spawned()
		{
			base.Spawned();

			if (Instance != null)
			{
				Debug.LogError("BehaviourModifierManager.Spawned: Instance is not null");
				return;
			}

			Instance = this;

			_modifierContexts = new object[ModifierCapacity];
			networkTargets = new List<NetworkObjectContext>();

			if (HasStateAuthority)
				_deletingTargets = new Queue<int>();

			_pseudoNetworkPrefabPools = new ReferencePool<GameObject>[PseudoNetworkPrefabs.Length];
			for (int i = 0; i < PseudoNetworkPrefabs.Length; i++)
				_pseudoNetworkPrefabPools[i] = new GameObjectPool(PseudoNetworkPrefabs[i]);

			_modifiersDataCopy = new BehaviourModifierData[ModifierCapacity];
		}

		public BehaviourModifierData GetModifierData(int index)
		{
			System.Diagnostics.Debug.Assert(HasStateAuthority);
			return ModifiersData[index];
		}

		public void SetModifierData(int index, in BehaviourModifierData data)
		{
			System.Diagnostics.Debug.Assert(HasStateAuthority);
			ModifiersData.Set(index, data);
		}

		public int GetIndexOfBehaviour<T>() where T : BehaviourModifier
		{
			for (int i = 0; i < ModifierScripts.Length; i++)
			{
				if (ModifierScripts[i] is T)
					return ModifierScripts[i].ScriptIndex;
			}

			return -1;
		}

		private int ReserveFreeModifierIndex(int scriptIndex, int targetIndex)
		{
			for (int i = 0; i < ModifierCapacity; i++)
			{
				if (ModifiersData[i].IsValid) continue;
				if (ModifiersData[i].ScriptType == scriptIndex && ModifiersData[i].TargetIndex == targetIndex)
					continue;

				if (i >= modifiersDataUsingCapacity)
					modifiersDataUsingCapacity = i + 1;

				return i;
			}

			return -1;
		}

		private int ReserveFreeNetworkIndex()
		{
			for (int i = 0; i < networkTargets.Count; i++)
			{
				if (!networkTargets[i].IsReserved)
				{
					networkTargets[i].IsReserved = true;
					return i;
				}
			}

			networkTargets.Add(new NetworkObjectContext());
			networkTargets[networkTargets.Count - 1].IsReserved = true;
			return networkTargets.Count - 1;
		}

		public int GetScriptIndex(BehaviourModifier script) => System.Array.IndexOf(ModifierScripts, script);

		public void AddModifier(ref BehaviourModifierData data, BehaviourModifier modifierScript = null, GameObject pseudoNetworkPrefab = null)
		{
			PlaceModifier(-1, ref data, modifierScript, pseudoNetworkPrefab);
		}

		public void PlaceModifier(int modifierAddress, ref BehaviourModifierData data, BehaviourModifier modifierScript = null, GameObject pseudoNetworkPrefab = null)
		{
			try
			{
				if (modifierScript != null) data.ScriptType = modifierScript.ScriptIndex;
				System.Diagnostics.Debug.Assert(data.ScriptType < ModifierScripts.Length, "BehaviourModifierManager.ReplaceModifier: data.scriptType is out of range");

				if (pseudoNetworkPrefab != null) data.PrefabIndex = System.Array.IndexOf(PseudoNetworkPrefabs, pseudoNetworkPrefab);
				System.Diagnostics.Debug.Assert(data.PrefabIndex < PseudoNetworkPrefabs.Length, "BehaviourModifierManager.ReplaceModifier: data.prefabIndex is out of range");

				if (data.TargetIndex == -1) data.TargetIndex = ReserveFreeNetworkIndex();
				System.Diagnostics.Debug.Assert(data.TargetIndex < ModifierCapacity, "BehaviourModifierManager.ReplaceModifier: parameter data.targetIndex is out of range: Range is [0, " + ModifierCapacity + "), but data.targetIndex is " + data.TargetIndex);

				if (modifierAddress == -1)
				{
					modifierAddress = ReserveFreeModifierIndex(data.ScriptType, data.TargetIndex);
					System.Diagnostics.Debug.Assert(modifierAddress != -1, "BehaviourModifierManager.AddModifier: ModifierCapacity (" + ModifierCapacity + ") reached. Modifier will not be added.");
				}


				UnityEngine.Debug.Log("BehaviourModifierManager.PlaceModifier: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

				ModifiersData.Set(modifierAddress, data);
				_modifierContexts[modifierAddress] = null;
			}
			catch (System.Exception e)
			{
				Debug.LogError(e);
			}
		}


		public void InitializeContext(int modifierAddress)
		{
			BehaviourModifierData data = ModifiersData[modifierAddress];
			NetworkObjectContext target = GetOrCreateTarget(data.PrefabIndex, data.TargetIndex);

			ModifierScripts[data.ScriptType].Initialize(target, in data, out _modifierContexts[modifierAddress]);

			if (_modifierContexts[modifierAddress] == null)
			{
				Debug.LogWarning("BehaviourModifierManager.FixedUpdateNetwork: " + ModifierScripts[data.ScriptType].GetType().Name + ".Initialize(" + gameObject + ") returned null context. This will cause the target object to be searched for every FixedUpdateNetwork and render.");
			}

			System.Diagnostics.Debug.Assert(target.PrefabIndex == data.PrefabIndex, "BehaviourModifierManager.InitializeContext: PrefabIndex mismatch from target context! target (" + target.PrefabIndex + ") != modifier (" + data.PrefabIndex + ")");

			target.Modifiers.Add(modifierAddress);
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (!HasStateAuthority) return;

			if (_deletingTargets.Count > 0)
			{
				UnityEngine.Debug.LogError("BehaviourModifierManager.FixedUpdateNetwork: _deletingTargets.Count > 0");
			}

			for (int modifierAddress = 0; modifierAddress < modifiersDataUsingCapacity; modifierAddress++)
			{
				BehaviourModifierData data = ModifiersData[modifierAddress];
				if (!data.IsValid) continue;

				if (_modifierContexts[modifierAddress] == null)
					InitializeContext(modifierAddress);

				ModifierScripts[data.ScriptType].OnFixedUpdate(_modifierContexts[modifierAddress], ref data);
				ModifiersData.Set(modifierAddress, data);
			}

			while (_deletingTargets.Count > 0)
			{
				int targetIndex = _deletingTargets.Dequeue();

				foreach (int modifierAddress in networkTargets[targetIndex].Modifiers)
				{
					if (!ModifiersData[modifierAddress].IsValid) continue;
					if (ModifiersData[modifierAddress].TargetIndex != targetIndex) continue;

					BehaviourModifierData data = ModifiersData[modifierAddress];
					ModifierScripts[ModifiersData[modifierAddress].ScriptType].OnInvalidatedUpdate(_modifierContexts[modifierAddress], ref data);
					ModifiersData.Set(modifierAddress, data);

					UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (update) Modifier due to delete: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

				}
			}
		}

		public override void Render()
		{
			base.Render();

			for (int modifierAddress = 0; modifierAddress < modifiersDataUsingCapacity; modifierAddress++)
			{
				BehaviourModifierData current = ModifiersData[modifierAddress];
				BehaviourModifierData previous = _modifiersDataCopy[modifierAddress];
				if (_modifierContexts[modifierAddress] != null) // Checks if the modifier was previously rendered
				{
					System.Diagnostics.Debug.Assert(previous.IsValid, "BehaviourModifierManager.Render: previous.IsValid is false at the same time as _modifierContexts[modifierAddress] is not null. This should never happen.");

					if (previous.ScriptType != current.ScriptType ||
						previous.TargetIndex != current.TargetIndex ||
						!current.IsValid)
					{
						ModifierScripts[previous.ScriptType].OnInvalidatedRender(_modifierContexts[modifierAddress], in previous);
						_modifierContexts[modifierAddress] = null;

						networkTargets[ModifiersData[modifierAddress].TargetIndex].Modifiers.Remove(modifierAddress);

						UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (render): { address: " + modifierAddress + ", scriptType: " + previous.ScriptType + ", prefabIndex: " + previous.PrefabIndex + ", targetIndex: " + previous.TargetIndex + " }");
					}
				}

				_modifiersDataCopy[modifierAddress] = current;
			}

			// Note that the objects are returned before executing the normal render because if a modifier was removed and replace with another on the same update, the new modifier could possibly target the same object index but be intended for a different object
			for (int i = 0; i < networkTargets.Count; i++)
			{
				if (!networkTargets[i].IsReserved) continue;
				if (networkTargets[i].Target == null) continue; // When reserved but has not been rendered yet
				if (networkTargets[i].Modifiers.Count > 0) continue;

				ReturnTargetObject(i);
			}

			for (int modifierAddress = 0; modifierAddress < modifiersDataUsingCapacity; modifierAddress++)
			{
				if (!ModifiersData[modifierAddress].IsValid) continue;
				if (_modifierContexts[modifierAddress] == null)
					InitializeContext(modifierAddress);
			}

			for (int modifierAddress = 0; modifierAddress < modifiersDataUsingCapacity; modifierAddress++)
			{
				BehaviourModifierData data = ModifiersData[modifierAddress];
				if (!data.IsValid) continue;

				ModifierScripts[data.ScriptType].OnRender(_modifierContexts[modifierAddress], in data);
			}
		}

		
		public bool NetworkContextIsValid(int index) => TryGetNetworkContext(index, out _);
		public bool TryGetNetworkContext(int index, out NetworkObjectContext target)
		{
			if (index >= networkTargets.Count)
			{
				target = null;
				return false;
			}

			return (target = networkTargets[index]).Target != null;
		}

		public NetworkObjectContext GetOrCreateTarget(int prefab, int index)
		{
			if (!TryGetNetworkContext(index, out NetworkObjectContext target))
				CreateTargetObjectAt(prefab, index, out target);
			return target;
		}

		public int CreateTargetObject(int prefab) => CreateTargetObject(prefab, out _);

		public int CreateTargetObject(int prefab, out NetworkObjectContext target)
		{
			int openSlot = networkTargets.IndexOf(null);

			if (openSlot == -1)
			{
				openSlot = networkTargets.Count;
			}

			CreateTargetObjectAt(prefab, openSlot, out target);
			return openSlot;
		}

		public void CreateTargetObjectAt(int prefab, int index) => CreateTargetObjectAt(prefab, index, out _);

		public void CreateTargetObjectAt(int prefab, int index, out NetworkObjectContext context)
		{
			if (TryGetNetworkContext(index, out context))
			{
				Debug.LogWarning("BehaviourModifierManager.CreateTargetObjectAt: target at index " + index + " already exists ("  + context.Target.name + ")");
				return;
			}

			GameObject target = _pseudoNetworkPrefabPools[prefab].Get();
			target.SetActive(true);
			Runner.MoveToRunnerScene(target);

			while (networkTargets.Count <= index)
				networkTargets.Add(new NetworkObjectContext());

			networkTargets[index].Target = target;
			networkTargets[index].PrefabIndex = prefab;
		}

		public bool IsQueuedForDeletion(int index) => _deletingTargets.Contains(index);

		public bool QueueDeleteTargetObject(int index)
		{
			if (IsQueuedForDeletion(index))
				return false;

			_deletingTargets.Enqueue(index);
			return true;
		}

		private void ReturnTargetObject(int targetIndex)
		{
			if (!TryGetNetworkContext(targetIndex, out NetworkObjectContext context))
			{
				Debug.LogError("BehaviourModifierManager.DestroyTargetObject: target at index " + targetIndex + " does not exist.");
				return;
			}

			ReturnTargetObject(context);
		}

		private void ReturnTargetObject(NetworkObjectContext context)
		{
			System.Diagnostics.Debug.Assert(context.Target != null, "BehaviourModifierManager.DestroyTargetObject: context.Target is null.");
			System.Diagnostics.Debug.Assert(context.Modifiers.Count == 0, "BehaviourModifierManager.DestroyTargetObject: context.Modifiers.Count > 0.");

			context.Target.SetActive(false);
			_pseudoNetworkPrefabPools[context.PrefabIndex].Return(context.Target);
			context.Target = null;
			context.PrefabIndex = -1;
			context.IsReserved = false;
		}
	}
}