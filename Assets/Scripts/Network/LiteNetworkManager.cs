using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkManager : NetworkBehaviour
	{
		private struct LiteDataBuffer : INetworkStruct
		{
			public unsafe fixed byte CustomData[LiteNetworkedData.CustomDataSize + LiteNetworkedData.CustomDataOffset];

			public static implicit operator LiteNetworkedData(LiteDataBuffer buffer)
			{
				unsafe
				{
					return *(LiteNetworkedData*)(&buffer);
				};
			}

			public static implicit operator LiteDataBuffer(LiteNetworkedData data)
			{
				unsafe
				{
					return *(LiteDataBuffer*)(&data);
				};
			}
		}

		private static LiteNetworkManager self;

		public const int LiteModCapacity = 256;
		[SerializeField] private LiteNetworkModScripts _liteNetworkModScripts;
		[SerializeField] private StateBehaviourController[] LiteNetworkPrefabs;

		public LiteNetworkMod[] ModScripts => _liteNetworkModScripts?.Mods ?? new LiteNetworkMod[0];

		[Networked, Capacity(LiteModCapacity)]
		private NetworkArray<LiteDataBuffer> LiteModData { get; }
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

		public static int CreateRawNetworkObjectContext() => self.ReserveFreeNetworkIndex();
		public static int CreateRawNetworkObjectContext(out LiteNetworkedData data, StateBehaviourController prefab)
		{
			int target = LiteNetworkManager.CreateRawNetworkObjectContext();
			data = default;
			data.TargetIndex = target;
			data.PrefabIndex = prefab.PrefabIndex;

			return target;
		}

		public static void CreateNetworkObjectContext(GameObject prefabComp, TransformData transform = default) =>
			CreateNetworkObjectContext(prefabComp.GetComponent<StateBehaviourController>(), transform);
		public static void CreateNetworkObjectContext(out LiteNetworkedData data, GameObject prefabComp, TransformData transform = default) =>
			CreateNetworkObjectContext(out data, prefabComp.GetComponent<StateBehaviourController>(), transform);
		public static void CreateNetworkObjectContext(Component prefabComp, TransformData transform = default) =>
			CreateNetworkObjectContext(prefabComp.GetComponent<StateBehaviourController>(), transform);
		public static void CreateNetworkObjectContext(out LiteNetworkedData data, Component prefabComp, TransformData transform = default) =>
			CreateNetworkObjectContext(out data, prefabComp.GetComponent<StateBehaviourController>(), transform);
		public static void CreateNetworkObjectContext(StateBehaviourController prefab, TransformData transform = default) => CreateNetworkObjectContext(out _, prefab, transform);
		public static void CreateNetworkObjectContext(out LiteNetworkedData data, StateBehaviourController prefab, TransformData transform = default)
		{
			int target = CreateRawNetworkObjectContext(out data, prefab);
			AddDefaultModifiers(target, prefab.PrefabIndex, transform);
		}

		public static void AddDefaultModifiers(int targetIndex, int prefabIndex, TransformData transform)
		{
			StateBehaviourController prefab = self.LiteNetworkPrefabs[prefabIndex];

			if (transform.Scale == Vector2.zero)
				transform.Scale = prefab.transform.localScale;

			LiteNetworkedData[] modifiersData = prefab.GetDefaultMods(ref transform);
			int lastModifierAddress = -1;

			for (int i = 0; i < modifiersData.Length; i++)
			{
				modifiersData[i].TargetIndex = targetIndex;
				modifiersData[i].PrefabIndex = prefabIndex;

				LiteNetworkMod script = self.ModScripts[modifiersData[i].ScriptType];

				script.SetRuntime(ref modifiersData[i]);

				if (script is TransformMod transformModifier)
					transformModifier.SetTransformData(ref modifiersData[i], transform);

				lastModifierAddress = self.ReserveFreeModifierIndex(modifiersData[i].ScriptType, modifiersData[i].TargetIndex, lastModifierAddress);
				PlaceModifier(lastModifierAddress, in modifiersData[i]);
			}
		}

		private int ReserveFreeModifierIndex(int scriptIndex, int targetIndex, int mustBeAfter = -1)
		{
			for (int i = mustBeAfter + 1; i < LiteModCapacity; i++)
			{
				LiteNetworkedData data = LiteModData[i];
				if (data.IsValid) continue;
				if (data.ScriptType == scriptIndex && data.TargetIndex == targetIndex)
					continue;

				if (i >= liteModUsingCapacity)
					liteModUsingCapacity = i + 1;

				return i;
			}

			Debug.LogWarning("BehaviourModifierManager.ReserveFreeModifierIndex: No free modifier index found, expanding capacity");
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

		public static void AddModifier(in LiteNetworkedData data)
		{
			int modifierAddress = self.ReserveFreeModifierIndex(data.ScriptType, data.TargetIndex);
			PlaceModifier(modifierAddress, in data);
		}

		public static void PlaceModifier(int modifierAddress, in LiteNetworkedData data)
		{
			self._PlaceModifier(modifierAddress, in data);
		}

		private void _PlaceModifier(int modifierAddress, in LiteNetworkedData data)
		{
			try
			{
				System.Diagnostics.Debug.Assert(HasStateAuthority);
				System.Diagnostics.Debug.Assert(data.ScriptType < ModScripts.Length, "BehaviourModifierManager.ReplaceModifier: data.scriptType is out of range");
				System.Diagnostics.Debug.Assert(data.PrefabIndex < LiteNetworkPrefabs.Length, "BehaviourModifierManager.ReplaceModifier: data.prefabIndex is out of range");
				System.Diagnostics.Debug.Assert(data.TargetIndex < LiteModCapacity, "BehaviourModifierManager.ReplaceModifier: parameter data.targetIndex is out of range: Range is [0, " + LiteModCapacity + "), but data.targetIndex is " + data.TargetIndex);
				System.Diagnostics.Debug.Assert(modifierAddress != -1, "BehaviourModifierManager.AddModifier: ModifierCapacity (" + LiteModCapacity + ") reached. Modifier will not be added.");
				// UnityEngine.Debug.Log("BehaviourModifierManager.PlaceModifier: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

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

			ModScripts[data.ScriptType].Initialize(target, in data, out liteModContexts[modifierAddress]);

			if (liteModContexts[modifierAddress] == null)
			{
				Debug.LogWarning("BehaviourModifierManager.FixedUpdateNetwork: " + ModScripts[data.ScriptType].GetType().Name + ".Initialize(" + gameObject + ") returned null context. This will cause the target object to be searched for every FixedUpdateNetwork and render.");
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

				ModScripts[data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref data);
				LiteModData.Set(modifierAddress, data);
			}

			while (_deletingLiteObjects.Count > 0)
			{
				int targetIndex = _deletingLiteObjects.Dequeue();

				foreach (int modifierAddress in liteNetworkObjects[targetIndex].Modifiers)
				{
					LiteNetworkedData data = LiteModData[modifierAddress];
					if (!data.IsValid) continue;
					if (data.TargetIndex != targetIndex) continue;

					ModScripts[data.ScriptType].OnInvalidatedUpdate(liteModContexts[modifierAddress], ref data);
					LiteModData.Set(modifierAddress, data);

					// UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (update) Modifier due to delete: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

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
				if (previous.IsValid)
				{
					if (previous.ScriptType != current.ScriptType ||
						previous.TargetIndex != current.TargetIndex ||
						!current.IsValid)
					{
						if (previous.TargetIndex < liteNetworkObjects.Count && // If target was created
							liteNetworkObjects[previous.TargetIndex].IsReserved && // If target still exits
							liteNetworkObjects[previous.TargetIndex].Modifiers.Remove(modifierAddress)) // If target still has this modifier
						{
							ModScripts[previous.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in previous);
							liteModContexts[modifierAddress] = null;
						}
					}
				}

				int targetForAddress = -1;
				for (int i = 0; i < liteNetworkObjects.Count; i++)
				{
					if (liteNetworkObjects[i].IsReserved && liteNetworkObjects[i].Modifiers.Contains(modifierAddress))
					{
						targetForAddress = i;
						break;
					}
				}

				if (targetForAddress != -1)
				{
					var target = liteNetworkObjects[targetForAddress];
					if (current.TargetIndex != targetForAddress)
					{
						UnityEngine.Debug.LogWarning($"Add: {modifierAddress} -> Previous: {previous.ToString()} ;;;;; Current: {current.ToString()}");
					}
				}

				liteModDataCopy[modifierAddress] = current;
			}

			// Note that the objects are returned before executing the normal render because if a modifier was removed and replace with another on the same update, the new modifier could possibly target the same object index but be intended for a different object
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				// UnityEngine.Debug.Log($"Checking {i}: {liteNetworkObjects[i].IsReserved} && {liteNetworkObjects[i].Target == null} && {liteNetworkObjects[i].Modifiers.Count} > 0 ;;;;; {liteNetworkObjects[i].Modifiers.Format()}");

				if (!liteNetworkObjects[i].IsReserved) continue;
				if (liteNetworkObjects[i].Target == null) continue; // When reserved but has not been rendered yet
				if (liteNetworkObjects[i].Modifiers.Count > 0) continue;


				ReleaseLiteObject(i);
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;
				if (liteModContexts[modifierAddress] == null)
					InitializeContext(modifierAddress);
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				ModScripts[data.ScriptType].OnRender(liteModContexts[modifierAddress], in data);
			}
		}

		public static bool TryGetNetworkContext(int index, out LiteNetworkObject target)
		{
			return self._TryGetNetworkContext(index, out target);
		}

		
		public static bool NetworkContextIsValid(int index) => TryGetNetworkContext(index, out _);
		public bool _TryGetNetworkContext(int index, out LiteNetworkObject target)
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
			GameObject target = GameObject.Instantiate(LiteNetworkPrefabs[prefab].gameObject);
			Runner.MoveToRunnerScene(target);

			while (liteNetworkObjects.Count <= index)
				liteNetworkObjects.Add(new LiteNetworkObject());

			liteNetworkObjects[index].Target = target;
			liteNetworkObjects[index].PrefabIndex = prefab;
		}

		public static bool IsQueuedForDeletion(int index) => self._deletingLiteObjects.Contains(index);

		public static bool QueueDeleteLiteObject(int index)
		{
			System.Diagnostics.Debug.Assert(self.HasStateAuthority, "BehaviourModifierManager.QueueDeleteLiteObject: HasStateAuthority is false.");
			System.Diagnostics.Debug.Assert(self.Runner.Stage != default, "BehaviourModifierManager.QueueDeleteLiteObject: Runner.Stage is not fixed update");

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

#if UNITY_EDITOR
		private void OnValidate()
		{
			StateRunnerMod stateRunnerMod =
				System.Array.Find(ModScripts, (LiteNetworkMod mod) => mod is StateRunnerMod) as StateRunnerMod;
			if (stateRunnerMod == null) return;

			for (int i = 0; i < LiteNetworkPrefabs.Length; i++)
			{
				if (LiteNetworkPrefabs[i] == null) continue;

				LiteNetworkPrefabs[i].SetHiddenSerializedFields(i, stateRunnerMod);
			}
		}
#endif
	}
}