using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkObject
	{
		public bool IsReserved;
		public List<int> Modifiers;
		public GameObject Target;
		public int PrefabIndex;

		public LiteNetworkObject()
		{
			IsReserved = false;
			Modifiers = new List<int>();
			Target = null;
			PrefabIndex = -1;
		}
	}

	public class LiteNetworkManager : NetworkBehaviour
	{
		private static LiteNetworkManager self;

		public const int LiteModCapacity = 96;

		[SerializeField] private LiteNetworkMod[] LiteModScripts;
		[SerializeField] private GameObject[] LiteNetworkPrefabs;

		[Networked, Capacity(LiteModCapacity)]
		private NetworkArray<LiteNetworkedData> LiteModData { get; }
		/// <summary>
		/// The highest index in ModifiersData that is currently in use.
		/// Used as a hint to avoid iterating over the entire array (performance).
		/// </summary>
		[Networked] private int liteModUsingCapacity { get; set; }
		// TODO: This is to make sure that if an index is disabled and enabled with different data, we can clear the data first.
		private LiteNetworkedData[] liteModDataCopy;

		// private ReferencePool<GameObject>[] _pseudoNetworkPrefabPools;

		private object[] liteModContexts;

		private List<LiteNetworkObject> liteNetworkObjects;

		private Queue<int> _deletingLiteObjects;

		public override void Spawned()
		{
			base.Spawned();

			if (self != null)
			{
				Debug.LogError("BehaviourModifierManager.Spawned: Instance is not null");
				return;
			}

			self = this;

			liteModContexts = new object[LiteModCapacity];
			liteNetworkObjects = new List<LiteNetworkObject>();

			if (HasStateAuthority)
				_deletingLiteObjects = new Queue<int>();

			// _pseudoNetworkPrefabPools = new ReferencePool<GameObject>[LiteNetworkPrefabs.Length];
			// for (int i = 0; i < LiteNetworkPrefabs.Length; i++)
			// 	_pseudoNetworkPrefabPools[i] = new GameObjectPool(LiteNetworkPrefabs[i]);

			liteModDataCopy = new LiteNetworkedData[LiteModCapacity];
		}

		public static LiteNetworkedData GetModifierData(int index)
		{
			System.Diagnostics.Debug.Assert(self.HasStateAuthority);
			return self.LiteModData[index];
		}

		public static void SetModifierData(int index, in LiteNetworkedData data)
		{
			System.Diagnostics.Debug.Assert(self.HasStateAuthority);
			self.LiteModData.Set(index, data);
		}

		public static int GetIndexOfBehaviour<T>() where T : LiteNetworkMod
		{
			for (int i = 0; i < self.LiteModScripts.Length; i++)
			{
				if (self.LiteModScripts[i] is T)
					return self.LiteModScripts[i].ScriptIndex;
			}

			return -1;
		}

		private int ReserveFreeModifierIndex(int scriptIndex, int targetIndex)
		{
			for (int i = 0; i < LiteModCapacity; i++)
			{
				if (LiteModData[i].IsValid) continue;
				if (LiteModData[i].ScriptType == scriptIndex && LiteModData[i].TargetIndex == targetIndex)
					continue;

				if (i >= liteModUsingCapacity)
					liteModUsingCapacity = i + 1;

				return i;
			}

			return -1;
		}

		private int ReserveFreeNetworkIndex()
		{
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (!liteNetworkObjects[i].IsReserved)
				{
					liteNetworkObjects[i].IsReserved = true;
					return i;
				}
			}

			liteNetworkObjects.Add(new LiteNetworkObject());
			liteNetworkObjects[liteNetworkObjects.Count - 1].IsReserved = true;
			return liteNetworkObjects.Count - 1;
		}

		public static int GetScriptIndex(LiteNetworkMod script) => System.Array.IndexOf(self.LiteModScripts, script);

		public static void AddModifier(ref LiteNetworkedData data, LiteNetworkMod modifierScript = null, GameObject pseudoNetworkPrefab = null)
		{
			PlaceModifier(-1, ref data, modifierScript, pseudoNetworkPrefab);
		}

		public static void PlaceModifier(int modifierAddress, ref LiteNetworkedData data, LiteNetworkMod modifierScript = null, GameObject pseudoNetworkPrefab = null)
		{
			System.Diagnostics.Debug.Assert(self.HasStateAuthority);
			self._PlaceModifier(modifierAddress, ref data, modifierScript, pseudoNetworkPrefab);
		}

		private void _PlaceModifier(int modifierAddress, ref LiteNetworkedData data, LiteNetworkMod modifierScript = null, GameObject pseudoNetworkPrefab = null)
		{
			try
			{
				if (modifierScript != null) data.ScriptType = modifierScript.ScriptIndex;
				System.Diagnostics.Debug.Assert(data.ScriptType < LiteModScripts.Length, "BehaviourModifierManager.ReplaceModifier: data.scriptType is out of range");

				if (pseudoNetworkPrefab != null) data.PrefabIndex = System.Array.IndexOf(LiteNetworkPrefabs, pseudoNetworkPrefab);
				System.Diagnostics.Debug.Assert(data.PrefabIndex < LiteNetworkPrefabs.Length, "BehaviourModifierManager.ReplaceModifier: data.prefabIndex is out of range");

				if (data.TargetIndex == -1) data.TargetIndex = ReserveFreeNetworkIndex();
				System.Diagnostics.Debug.Assert(data.TargetIndex < LiteModCapacity, "BehaviourModifierManager.ReplaceModifier: parameter data.targetIndex is out of range: Range is [0, " + LiteModCapacity + "), but data.targetIndex is " + data.TargetIndex);

				if (modifierAddress == -1)
				{
					modifierAddress = ReserveFreeModifierIndex(data.ScriptType, data.TargetIndex);
					System.Diagnostics.Debug.Assert(modifierAddress != -1, "BehaviourModifierManager.AddModifier: ModifierCapacity (" + LiteModCapacity + ") reached. Modifier will not be added.");
				}


				UnityEngine.Debug.Log("BehaviourModifierManager.PlaceModifier: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

				LiteModData.Set(modifierAddress, data);
				liteModContexts[modifierAddress] = null;
			}
			catch (System.Exception e)
			{
				Debug.LogError(e);
			}
		}


		public void InitializeContext(int modifierAddress)
		{
			LiteNetworkedData data = LiteModData[modifierAddress];
			LiteNetworkObject target = GetOrCreatePrefab(data.PrefabIndex, data.TargetIndex);

			LiteModScripts[data.ScriptType].Initialize(target, in data, out liteModContexts[modifierAddress]);

			if (liteModContexts[modifierAddress] == null)
			{
				Debug.LogWarning("BehaviourModifierManager.FixedUpdateNetwork: " + LiteModScripts[data.ScriptType].GetType().Name + ".Initialize(" + gameObject + ") returned null context. This will cause the target object to be searched for every FixedUpdateNetwork and render.");
			}

			System.Diagnostics.Debug.Assert(target.PrefabIndex == data.PrefabIndex, "BehaviourModifierManager.InitializeContext: PrefabIndex mismatch from target context! target (" + target.PrefabIndex + ") != modifier (" + data.PrefabIndex + ")");

			target.Modifiers.Add(modifierAddress);
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (!HasStateAuthority) return;

			if (_deletingLiteObjects.Count > 0)
			{
				UnityEngine.Debug.LogError("BehaviourModifierManager.FixedUpdateNetwork: _deletingTargets.Count > 0");
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				if (liteModContexts[modifierAddress] == null)
					InitializeContext(modifierAddress);

				LiteModScripts[data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref data);
				LiteModData.Set(modifierAddress, data);
			}

			while (_deletingLiteObjects.Count > 0)
			{
				int targetIndex = _deletingLiteObjects.Dequeue();

				foreach (int modifierAddress in liteNetworkObjects[targetIndex].Modifiers)
				{
					if (!LiteModData[modifierAddress].IsValid) continue;
					if (LiteModData[modifierAddress].TargetIndex != targetIndex) continue;

					LiteNetworkedData data = LiteModData[modifierAddress];
					LiteModScripts[LiteModData[modifierAddress].ScriptType].OnInvalidatedUpdate(liteModContexts[modifierAddress], ref data);
					LiteModData.Set(modifierAddress, data);

					UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (update) Modifier due to delete: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

				}
			}
		}

		public override void Render()
		{
			base.Render();

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData current = LiteModData[modifierAddress];
				LiteNetworkedData previous = liteModDataCopy[modifierAddress];
				if (liteModContexts[modifierAddress] != null) // Checks if the modifier was previously rendered
				{
					System.Diagnostics.Debug.Assert(previous.IsValid, "BehaviourModifierManager.Render: previous.IsValid is false at the same time as _modifierContexts[modifierAddress] is not null. This should never happen.");

					if (previous.ScriptType != current.ScriptType ||
						previous.TargetIndex != current.TargetIndex ||
						!current.IsValid)
					{
						LiteModScripts[previous.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in previous);
						liteModContexts[modifierAddress] = null;

						liteNetworkObjects[LiteModData[modifierAddress].TargetIndex].Modifiers.Remove(modifierAddress);

						UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (render): { address: " + modifierAddress + ", scriptType: " + previous.ScriptType + ", prefabIndex: " + previous.PrefabIndex + ", targetIndex: " + previous.TargetIndex + " }");
					}
				}

				liteModDataCopy[modifierAddress] = current;
			}

			// Note that the objects are returned before executing the normal render because if a modifier was removed and replace with another on the same update, the new modifier could possibly target the same object index but be intended for a different object
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (!liteNetworkObjects[i].IsReserved) continue;
				if (liteNetworkObjects[i].Target == null) continue; // When reserved but has not been rendered yet
				if (liteNetworkObjects[i].Modifiers.Count > 0) continue;

				ReleaseLiteObject(i);
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				if (!LiteModData[modifierAddress].IsValid) continue;
				if (liteModContexts[modifierAddress] == null)
					InitializeContext(modifierAddress);
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				LiteModScripts[data.ScriptType].OnRender(liteModContexts[modifierAddress], in data);
			}
		}

		
		public bool NetworkContextIsValid(int index) => TryGetNetworkContext(index, out _);
		public bool TryGetNetworkContext(int index, out LiteNetworkObject target)
		{
			if (index >= liteNetworkObjects.Count)
			{
				target = null;
				return false;
			}

			return (target = liteNetworkObjects[index]).Target != null;
		}

		public LiteNetworkObject GetOrCreatePrefab(int prefab, int index)
		{
			if (!TryGetNetworkContext(index, out LiteNetworkObject target))
				CreatePrefabOnLiteObject(prefab, index, out target);
			return target;
		}

		public int CreatePrefabOnLiteObject(int prefab) => CreatePrefabOnLiteObject(prefab, out _);

		public int CreatePrefabOnLiteObject(int prefab, out LiteNetworkObject target)
		{
			int openSlot = liteNetworkObjects.IndexOf(null);

			if (openSlot == -1)
			{
				openSlot = liteNetworkObjects.Count;
			}

			CreatePrefabOnLiteObject(prefab, openSlot, out target);
			return openSlot;
		}

		public void CreatePrefabOnLiteObject(int prefab, int index) => CreatePrefabOnLiteObject(prefab, index, out _);

		public void CreatePrefabOnLiteObject(int prefab, int index, out LiteNetworkObject context)
		{
			if (TryGetNetworkContext(index, out context))
			{
				Debug.LogWarning("BehaviourModifierManager.CreateTargetObjectAt: target at index " + index + " already exists ("  + context.Target.name + ")");
				return;
			}

			// GameObject target = _pseudoNetworkPrefabPools[prefab].Get();
			GameObject target = GameObject.Instantiate(LiteNetworkPrefabs[prefab]);
			Runner.MoveToRunnerScene(target);

			while (liteNetworkObjects.Count <= index)
				liteNetworkObjects.Add(new LiteNetworkObject());

			liteNetworkObjects[index].Target = target;
			liteNetworkObjects[index].PrefabIndex = prefab;
		}

		public static bool IsQueuedForDeletion(int index) => self._deletingLiteObjects.Contains(index);

		public static bool QueueDeleteLiteObject(int index)
		{
			if (IsQueuedForDeletion(index))
				return false;

			self._deletingLiteObjects.Enqueue(index);
			return true;
		}

		private void ReleaseLiteObject(int targetIndex)
		{
			if (!TryGetNetworkContext(targetIndex, out LiteNetworkObject context))
			{
				Debug.LogError("BehaviourModifierManager.DestroyTargetObject: target at index " + targetIndex + " does not exist.");
				return;
			}

			ReleaseLiteObject(context);
		}

		private void ReleaseLiteObject(LiteNetworkObject context)
		{
			System.Diagnostics.Debug.Assert(context.Target != null, "BehaviourModifierManager.DestroyTargetObject: context.Target is null.");
			System.Diagnostics.Debug.Assert(context.Modifiers.Count == 0, "BehaviourModifierManager.DestroyTargetObject: context.Modifiers.Count > 0.");

			GameObject.Destroy(context.Target);
			// context.Target.SetActive(false);
			// _pseudoNetworkPrefabPools[context.PrefabIndex].Return(context.Target);
			context.Target = null;
			context.PrefabIndex = -1;
			context.IsReserved = false;
		}
	}
}