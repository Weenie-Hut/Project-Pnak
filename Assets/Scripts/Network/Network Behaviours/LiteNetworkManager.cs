using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	[OrderBefore(typeof(Player))]
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

			public override string ToString()
			{
				return ((LiteNetworkedData)this).ToString();
			}
		}

		private static LiteNetworkManager self;

		public const int LiteModCapacity = 256;
		[SerializeField] private LiteNetworkModScripts _liteNetworkModScripts;

		[SerializeField, Attached] private LateNetworkUpdate LateUpdate;

		public LiteNetworkModScripts LiteNetworkModScripts => _liteNetworkModScripts;

		public static LiteNetworkMod[] ModScripts => self.LiteNetworkModScripts.Mods;
		// public static StateModifierSO[] StateModifiers => self.LiteNetworkModScripts.StateModifiers;
		public static StateBehaviourController[] Prefabs => self.LiteNetworkModScripts.LiteNetworkPrefabs;

		[Networked, Capacity(LiteModCapacity)]
		private NetworkArray<LiteDataBuffer> LiteModData { get; }

		[Networked(OnChanged = nameof(ChangedInputAuthorities)), Capacity(10)]
		private NetworkDictionary<ushort, PlayerRef> InputAuthorities { get; }

		/// <summary>
		/// The highest index in ModifiersData that is currently in use.
		/// Used as a hint to avoid iterating over the entire array (performance).
		/// </summary>
		[Networked] private ushort liteModUsingCapacity { get; set; }
		private ushort previousLiteModUsingCapacity = 0;
	
		// TODO: This is to make sure that if an index is disabled and enabled with different data, we can clear the data first.
		private LiteNetworkedData[] liteModDataCopy;

		// private ReferencePool<GameObject>[] _pseudoNetworkPrefabPools;

		private object[] liteModContexts;
		public static object GetModContext(int index) => self.liteModContexts[index];

		private List<LiteNetworkObject> liteNetworkObjects;

		private Queue<int> _deletingLiteObjects;

		private void Awake()
		{
			if (self != null)
			{
				Debug.LogError("BehaviourModifierManager.Spawned: Instance is not null");
				return;
			}
			self = this;

			liteModContexts = new object[LiteModCapacity];
			liteNetworkObjects = new List<LiteNetworkObject>();
			liteModDataCopy = new LiteNetworkedData[LiteModCapacity];
		}

		public override void Spawned()
		{
			base.Spawned();

			if (HasStateAuthority)
				_deletingLiteObjects = new Queue<int>();

			LateUpdate.OnFixedUpdate += LateFixedNetworkUpdate;

			// _pseudoNetworkPrefabPools = new ReferencePool<GameObject>[LiteNetworkPrefabs.Length];
			// for (int i = 0; i < LiteNetworkPrefabs.Length; i++)
			// 	_pseudoNetworkPrefabPools[i] = new GameObjectPool(LiteNetworkPrefabs[i]);
		}

		public static LiteNetworkedData GetModifierData(int index)
		{
			return self.LiteModData[index];
		}

		public static void SetModifierData(int index, in LiteNetworkedData data)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority);
			self.LiteModData.Set(index, data);
		}

		public static bool TryGetNetworkObject(int index, out LiteNetworkObject networkObject)
		{
			if (index < 0 || index >= self.liteNetworkObjects.Count || self.liteNetworkObjects[index] == null)
			{
				networkObject = null;
				return false;
			}

			networkObject = self.liteNetworkObjects[index];
			return true;
		}

		public static LiteNetworkObject TryGetNetworkObject(int index)
		{
			if (TryGetNetworkObject(index, out LiteNetworkObject networkObject))
				return networkObject;
			return null;
		}

		public static LiteNetworkObject GetNetworkObject(int index)
		{
			// if (!self.HasStateAuthority)
			// {
			// 	UnityEngine.Debug.LogError("GetNetworkObject is not safe on client nodes. Maybe make sure that objects are initialized on clients before this can be called?");
			// 	return null;
			// }

			try {
				return self.liteNetworkObjects[index];
			}
			catch (ArgumentOutOfRangeException)
			{
				UnityEngine.Debug.LogWarning($"GetNetworkObject: Index {index} is out of range");
				return null;
			}
		}

		public static TransformData? TryGetTransformData(LiteNetworkObject obj)
		{
			foreach (int mod in obj.Modifiers)
			{
				LiteNetworkedData data = GetModifierData(mod);
				if (ModScripts[data.ScriptType] is TransformMod transformMod)
					return transformMod.GetTransformData(GetModContext(mod), in data);
			}
			return null;
		}

		public class CreateNetworkObjectData
		{
			public StateBehaviourController Prefab;
			public TransformData Transform;
			public System.Action<LiteNetworkObject> AfterInitialize;
			public bool RunFirstUpdate;

			public CreateNetworkObjectData Set(StateBehaviourController prefab, TransformData transform, System.Action<LiteNetworkObject> afterInitialize, bool runFirstUpdate)
			{
				Prefab = prefab;
				Transform = transform;
				AfterInitialize = afterInitialize;
				RunFirstUpdate = runFirstUpdate;
				return this;
			}

			public void Clear()
			{
				Prefab = null;
				Transform = default;
				AfterInitialize = null;
			}
		}

		private ClassPool<CreateNetworkObjectData> _createNetworkObjectDataPool = new ClassPool<CreateNetworkObjectData>();
		private Queue<CreateNetworkObjectData> _createNetworkObjectQueue = new Queue<CreateNetworkObjectData>();

		public static void QueueNewNetworkObject(StateBehaviour prefab, TransformData transform = default, System.Action<LiteNetworkObject> afterInitialize = null, bool runFirstUpdate = true) =>
			QueueNewNetworkObject(prefab.Controller, transform, afterInitialize, runFirstUpdate);
		public static void QueueNewNetworkObject(StateBehaviourController prefab, TransformData transform = default, System.Action<LiteNetworkObject> afterInitialize = null, bool runFirstUpdate = true)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority, "Cannot queue new network objects on a manager without state auth.");
			// UnityEngine.Debug.Assert(self.Runner.Stage != default, "Cannot queue new network objects when not processing fixed updates.");

			self._createNetworkObjectQueue.Enqueue(self._createNetworkObjectDataPool.Get().Set(prefab, transform, afterInitialize, runFirstUpdate));
		}

#if DEBUG
		private const int MaxWorkingLoopCount = 2000;
		private int _workingLoopCount = 0;
		private void CheckInfiniteLoop()
		{
			if (_workingLoopCount++ > MaxWorkingLoopCount)
				throw new System.Exception("Infinite loop detected: \n" +
					"\tCreate Queue:" + _createNetworkObjectQueue.Format() + "\n" +
					"\tDelete Queue:" + _deletingLiteObjects.Format() + "\n" +
					"\tQueue Modifiers:" + _modifierQueue.Format() + "\n" +
					"\tLite Objects:" + liteNetworkObjects.Format() + "\n"
				);
		}
#endif

		public void RunQueueCreate()
		{
			while (_createNetworkObjectQueue.Count > 0)
			{
#if DEBUG
				CheckInfiniteLoop();
#endif
				CreateNetworkObjectData data = _createNetworkObjectQueue.Dequeue();
				int target = self.ReserveFreeNetworkIndex(data.Prefab.PrefabIndex);
				AddAndInitDefaultModifiers(target, data);

				data.Clear();
				_createNetworkObjectDataPool.Return(data);
			}
		}

		private void RunQueueDestroy()
		{
			while (_deletingLiteObjects.Count > 0)
			{
#if DEBUG
				CheckInfiniteLoop();
#endif
				int targetIndex = _deletingLiteObjects.Dequeue();

				for (int objectModifierIndex = 0; objectModifierIndex < liteNetworkObjects[targetIndex].Modifiers.Count; objectModifierIndex++)
				{
					int modifierAddress = liteNetworkObjects[targetIndex].Modifiers[objectModifierIndex];

					LiteNetworkedData data = LiteModData[modifierAddress];
					if (!data.IsValid) continue;
					if (data.TargetIndex != targetIndex) continue;

					// If the object was invalidated the same frame that it was initialized, only the state will have context
					if (InvalidatedUpdate(modifierAddress, ref data, objectModifierIndex))
						objectModifierIndex--;

					LiteModData.Set(modifierAddress, data);
				}

				if (liteNetworkObjects[targetIndex].InputAuthority != PlayerRef.None)
					RemoveInputAuthority(targetIndex);
			}
		}

		private List<int> _workingModAddresses = new List<int>();
		private void AddAndInitDefaultModifiers(int targetIndex, CreateNetworkObjectData data)
		{
			UnityEngine.Debug.Assert(data.Prefab.PrefabIndex >= 0, $"Prefab index {data.Prefab.PrefabIndex} ({data.Prefab.gameObject.name}) has not been registered.");
			UnityEngine.Debug.Assert(Prefabs.Length > data.Prefab.PrefabIndex, $"Prefab index {data.Prefab.PrefabIndex} ({data.Prefab.gameObject.name}) is out of range: {Prefabs.Length}");

			StateBehaviourController prefab = Prefabs[data.Prefab.PrefabIndex];

			if (data.Transform.Scale == Vector2.zero)
				data.Transform.Scale = prefab.transform.localScale;

			LiteNetworkedData[] modifiersData = prefab.GetDefaultMods(ref data.Transform);

#if DEBUG
			if (modifiersData.Length == 0)
			{
				// A note on the error: All object are networked using some form of modifier. Essentially, all modifier data is networked, and they all contain the object prefab and instance index, allowing all clients to be able to recreate the object if does not exist yet.
				UnityEngine.Debug.LogError($"Prefab {prefab.gameObject.name} ({prefab.PrefabIndex}) has no default modifiers. At least one modifier is required to create a networked object. Consider adding a transform modifier if you don't need any other modifiers.");
				return;
			}
#endif

			_workingModAddresses.Clear();
			int lastModifierAddress = -1;

			for (int i = 0; i < modifiersData.Length; i++)
			{
				modifiersData[i].TargetIndex = targetIndex;
				modifiersData[i].PrefabIndex = data.Prefab.PrefabIndex;

				LiteNetworkMod script = ModScripts[modifiersData[i].ScriptType];
				if (script is TransformMod transformModifier)
					transformModifier.SetTransformData(ref modifiersData[i], data.Transform);

				lastModifierAddress = self.ReserveFreeModifierIndex(modifiersData[i].ScriptType, modifiersData[i].TargetIndex, lastModifierAddress);
				PlaceModifier(lastModifierAddress, ref modifiersData[i]);
				self.InitializeContext(lastModifierAddress);

				_workingModAddresses.Add(lastModifierAddress);
			}

			liteNetworkObjects[targetIndex].Target.Initialize(liteNetworkObjects[targetIndex]);
			data.AfterInitialize?.Invoke(liteNetworkObjects[targetIndex]);

			if (data.RunFirstUpdate)
			{
				for (int i = 0; i < _workingModAddresses.Count; i++)
				{
					int modifierAddress = _workingModAddresses[i];
					LiteNetworkedData _data = LiteModData[modifierAddress];
					ModScripts[_data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref _data);
					LiteModData.Set(modifierAddress, _data);
				}

				liteNetworkObjects[targetIndex].Target.FixedUpdateNetwork();
			}
		}

		private int ReserveFreeModifierIndex(int scriptIndex, int targetIndex, int mustBeAfter = -1)
		{
			for (int i = mustBeAfter + 1; i < LiteModCapacity; i++)
			{
				LiteNetworkedData data = LiteModData[i];
				if (data.IsValid || liteModContexts[i] != null) continue;
				if (data.ScriptType == scriptIndex && data.TargetIndex == targetIndex)
					continue;

				if (i >= liteModUsingCapacity)
					liteModUsingCapacity = (ushort)(i + 1);

				return i;
			}

			Debug.LogWarning("BehaviourModifierManager.ReserveFreeModifierIndex: No free modifier index found, expanding capacity");
			return -1;
		}

		private int ReserveFreeNetworkIndex(int prefabIndex)
		{
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (!liteNetworkObjects[i].IsReserved)
				{
					liteNetworkObjects[i].STATE_ReserveAs(prefabIndex);
					return i;
				}
			}

			liteNetworkObjects.Add(new LiteNetworkObject(liteNetworkObjects.Count));
			liteNetworkObjects[liteNetworkObjects.Count - 1].STATE_ReserveAs(prefabIndex);
			return liteNetworkObjects.Count - 1;
		}

		private Queue<LiteNetworkedData> _modifierQueue = new Queue<LiteNetworkedData>();

		private void RunAddModifiers()
		{
			while (_modifierQueue.Count > 0)
			{
#if DEBUG
				CheckInfiniteLoop();
#endif
				LiteNetworkedData data = _modifierQueue.Dequeue();
				AddModifier(ref data);
			}
		}

		public static void QueueAddModifier(LiteNetworkObject networkObject, LiteNetworkedData data)
		{
			data.TargetIndex = networkObject.Index;
			data.PrefabIndex = networkObject.PrefabIndex;
			QueueAddModifier(data);
		}

		public static void QueueAddModifier(LiteNetworkedData data)
		{
			self._modifierQueue.Enqueue(data);
		}

		private void AddModifier(ref LiteNetworkedData data)
		{
			LiteNetworkObject networkObject = self.liteNetworkObjects[data.TargetIndex];

			UnityEngine.Debug.Assert(data.PrefabIndex == networkObject.PrefabIndex, $"LiteNetworkedData prefab index {data.PrefabIndex} does not match network object prefab index {networkObject.PrefabIndex}.");

			foreach (int existingAddress in networkObject.Modifiers)
			{
				LiteNetworkedData _data = self.LiteModData[existingAddress];
				if (ModScripts[_data.ScriptType].ModAdded_CombineWith(GetModContext(existingAddress), ref _data, in data))
					return;
			}

			int last = -1;
			if (networkObject.Modifiers.Count > 0)
				last = networkObject.Modifiers[networkObject.Modifiers.Count - 1];
			
			if (last == -1) {
				UnityEngine.Debug.LogWarning("BehaviourModifierManager.AddModifier: No existing object found, adding new object for modifier.");
			}

			int modifierAddress = self.ReserveFreeModifierIndex(data.ScriptType, data.TargetIndex, last);
			PlaceModifier(modifierAddress, ref data);

			InitializeContext(modifierAddress);
			ModScripts[data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref data);
			LiteModData.Set(modifierAddress, data);
		}

		public static void PlaceModifier(int modifierAddress, ref LiteNetworkedData data)
		{
			self._PlaceModifier(modifierAddress, ref data);
		}

		private void _PlaceModifier(int modifierAddress, ref LiteNetworkedData data)
		{
			try
			{
#if DEBUG
				UnityEngine.Debug.Assert(HasStateAuthority);
				UnityEngine.Debug.Assert(data.ScriptType < ModScripts.Length, "BehaviourModifierManager.ReplaceModifier: data.scriptType is out of range");
				UnityEngine.Debug.Assert(data.PrefabIndex < Prefabs.Length, "BehaviourModifierManager.ReplaceModifier: data.prefabIndex is out of range");
				UnityEngine.Debug.Assert(data.TargetIndex < LiteModCapacity, "BehaviourModifierManager.ReplaceModifier: parameter data.targetIndex is out of range: Range is [0, " + LiteModCapacity + "), but data.targetIndex is " + data.TargetIndex);
				UnityEngine.Debug.Assert(modifierAddress != -1, "BehaviourModifierManager.AddModifier: ModifierCapacity (" + LiteModCapacity + ") reached. Modifier will not be added.");

				LiteNetworkedData __data = LiteModData[modifierAddress];
				UnityEngine.Debug.Assert(!__data.IsValid, "BehaviourModifierManager.AddModifier: modifierAddress is not free");
				UnityEngine.Debug.Assert(liteModContexts[modifierAddress] == null, "BehaviourModifierManager.AddModifier: modifierAddress context is not null");
#endif

				ModScripts[data.ScriptType].SetRuntime(ref data);

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

			UnityEngine.Debug.Assert(target != null, "BehaviourModifierManager.InitializeContext: target is null");

			ModScripts[data.ScriptType].Initialize(target, in data, out liteModContexts[modifierAddress]);

			if (liteModContexts[modifierAddress] == null)
			{
				Debug.LogWarning("BehaviourModifierManager.FixedUpdateNetwork: " + ModScripts[data.ScriptType].GetType().Name + ".Initialize(" + gameObject + ") returned null context. This will cause the target object to be searched for every FixedUpdateNetwork and render.");
			}

			UnityEngine.Debug.Assert(target.PrefabIndex == data.PrefabIndex, "BehaviourModifierManager.InitializeContext: PrefabIndex mismatch from target context! target (" + target.PrefabIndex + ") != modifier (" + data.PrefabIndex + ")\n" + "Target: " + target.Format() + "\n" + data.ToString());

			// UnityEngine.Debug.Log("InitializeContext " + modifierAddress + ": " + data.ToString());

			target.Modifiers.Add(modifierAddress);
		}

		/// <summary>
		/// Runs the InvalidatedUpdate method, and removes the modifier IFF it was initialized and invalided in the same frame.
		/// </summary>
		/// <param name="modifierAddress"></param>
		/// <param name="data"></param>
		/// <param name="objectModifierIndex"></param>
		/// <returns>True if the modifier was removed from the object modifier list, false else.</returns>
		private bool InvalidatedUpdate(int modifierAddress, ref LiteNetworkedData data, int objectModifierIndex = -1)
		{
			ModScripts[data.ScriptType].OnInvalidatedUpdate(liteModContexts[modifierAddress], ref data);
			data.Invalidate();

			if (!liteModDataCopy[modifierAddress].IsValid)
			{
				if (objectModifierIndex != -1)
					liteNetworkObjects[data.TargetIndex].Modifiers.RemoveAt(objectModifierIndex);
				else liteNetworkObjects[data.TargetIndex].Modifiers.Remove(modifierAddress);
				ModScripts[data.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in data);
				
				liteModContexts[modifierAddress] = null;

				return true;
			}
			return false;
		}

		public void LateFixedNetworkUpdate()
		{
			if (!HasStateAuthority) return;

#if DEBUG
			_workingLoopCount = 0;
#endif

			// Recursively calls until queues are empty. Makes sure the all objects are deleted, created, initialized, and ran in correct order.
			while (
				_createNetworkObjectQueue.Count > 0 ||
				_deletingLiteObjects.Count > 0 ||
				_modifierQueue.Count > 0
			) {
#if DEBUG
				CheckInfiniteLoop();
#endif
				RunQueueCreate();
				RunAddModifiers();
				RunQueueDestroy();
			}
		}

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			if (!HasStateAuthority) return;

			// if (_deletingLiteObjects.Count > 0)
			// {
			// 	UnityEngine.Debug.LogError("BehaviourModifierManager.FixedUpdateNetwork: _deletingTargets.Count > 0");
			// }

			int lastValidAddress = -1;

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				lastValidAddress = modifierAddress;

				if (liteModContexts[modifierAddress] == null)
				{
					UnityEngine.Debug.LogError("BehaviourModifierManager.FixedUpdateNetwork: liteModContexts[" + modifierAddress + "] is null");
					InitializeContext(modifierAddress);
				}

				ModScripts[data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref data);
				if (!data.IsValid)
				{
					// Makes sure to remove the modifier from the target object IFF the modifier was invalidated the same frame it was initialized
					InvalidatedUpdate(modifierAddress, ref data);
				}
				LiteModData.Set(modifierAddress, data);
			}

			liteModUsingCapacity = (ushort)(lastValidAddress + 1);

			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (liteNetworkObjects[i].IsValid && !liteNetworkObjects[i].QueuedForDestruction)
					liteNetworkObjects[i].Target.InputFixedUpdateNetwork();
			}

			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (liteNetworkObjects[i].IsValid && !liteNetworkObjects[i].QueuedForDestruction)
					liteNetworkObjects[i].Target.FixedUpdateNetwork();
			}
		}

		public override void Render()
		{
			base.Render();

			int maxCapacity = previousLiteModUsingCapacity > liteModUsingCapacity ? previousLiteModUsingCapacity : liteModUsingCapacity;
			previousLiteModUsingCapacity = liteModUsingCapacity;

			for (int modifierAddress = 0; modifierAddress < maxCapacity; modifierAddress++)
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
							liteNetworkObjects[previous.TargetIndex].Target != null && // If target still exits
							liteNetworkObjects[previous.TargetIndex].Modifiers.Remove(modifierAddress)) // If target still has this modifier
						{
							ModScripts[previous.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in previous);
							liteModContexts[modifierAddress] = null;
						}
					}
				}
#if DEBUG
				else if (!HasStateAuthority && liteModContexts[modifierAddress] != null)
				{
					UnityEngine.Debug.LogError($"BehaviourModifierManager.Render: liteModContexts[{modifierAddress}] != null -> Previous: {previous.ToString()} ;;;;; Current: {current.ToString()} ;;;;; Context: {liteModContexts[modifierAddress].ToString()}");
				}
				
				UnityEngine.Debug.Assert(modifierAddress < liteModUsingCapacity || (!current.IsValid && liteModContexts[modifierAddress] == null), $"Render: current data outside of using address capacity is still valid, ie current is not invalid or context still exists: {previous.ToString()} ;;;;; Current: {current.ToString()}");

				int targetForAddress = -1;
				for (int i = 0; i < liteNetworkObjects.Count; i++)
				{
					if (liteNetworkObjects[i].IsValid && liteNetworkObjects[i].Modifiers.Contains(modifierAddress))
					{
						targetForAddress = i;
						break;
					}
				}

				if (targetForAddress != -1)
				{
					if (current.TargetIndex != targetForAddress)
					{
						var target = liteNetworkObjects[targetForAddress];
						UnityEngine.Debug.LogWarning($"Object {targetForAddress} which contains modifier address {modifierAddress} ({target.Modifiers.Format()}) does not match the current data target -> Previous: {previous.ToString()} ;;;;; Current: {current.ToString()}");
					}
				}
#endif

				liteModDataCopy[modifierAddress] = current;
			}

			// Note that the objects are returned before executing the normal render because if a modifier was removed and replace with another on the same update, the new modifier could possibly target the same object index but be intended for a different object
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (!liteNetworkObjects[i].IsReserved) continue;

				if (!liteNetworkObjects[i].IsValid)
					LOCAL_ReleaseLiteObject(i);
			}

			if (!HasStateAuthority)
			{
				for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
				{
					LiteNetworkedData data = LiteModData[modifierAddress];
					if (!data.IsValid) continue;
					if (liteModContexts[modifierAddress] == null)
						InitializeContext(modifierAddress);
				}
			}

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				try { ModScripts[data.ScriptType].OnRender(liteModContexts[modifierAddress], in data); }
				catch (System.Exception e)
				{
					UnityEngine.Debug.LogError(data.ToString() + " :: " + liteModContexts[modifierAddress].ToString());
					UnityEngine.Debug.LogError($"BehaviourModifierManager.Render: {e.Message}\n{e.StackTrace}");
				}
			}

			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				if (liteNetworkObjects[i].IsValid && !liteNetworkObjects[i].QueuedForDestruction)
				{
					if (liteNetworkObjects[i].Target.NetworkContext == null)
					{
						UnityEngine.Debug.Assert(!SessionManager.IsServer, "Server should not be creating/initialize new objects during render");
						liteNetworkObjects[i].Target.Initialize(liteNetworkObjects[i]);
					}

					liteNetworkObjects[i].Target.Render();
				}
			}
		}


		public static bool NetworkContextIsValid(int index)
		{
			if (!SessionManager.IsServer)
				if (index < 0 || index >= self.liteNetworkObjects.Count)
					return false;

			return self.liteNetworkObjects[index].IsValid;
		}

		public LiteNetworkObject GetOrCreatePrefab(int prefab, int index)
		{
			if (!NetworkContextIsValid(index))
				CreatePrefabOnLiteObject(prefab, index);

			return liteNetworkObjects[index];
		}

		private LiteNetworkObject GetOrPopulateObject(int index)
		{
#if DEBUG
			if (SessionManager.IsServer)
				// Makes sure that the index was reserved.
				UnityEngine.Debug.Assert(liteNetworkObjects.Count > index);
#endif
			while (liteNetworkObjects.Count <= index)
			{
				liteNetworkObjects.Add(new LiteNetworkObject(liteNetworkObjects.Count));
			}

			return liteNetworkObjects[index];
		}

		public void CreatePrefabOnLiteObject(int prefab, int index)
		{
#if DEBUG
			if (NetworkContextIsValid(index))
			{
				Debug.LogWarning("BehaviourModifierManager.CreateTargetObjectAt: target at index " + index + " already exists ("  + liteNetworkObjects[index].Target.name + ")");
				return;
			}
			
			if (SessionManager.IsServer)
				// Makes sure that the index was reserved.
				UnityEngine.Debug.Assert(liteNetworkObjects.Count > index);
#endif

			GameObject target = GameObject.Instantiate(Prefabs[prefab].gameObject);
			Runner.MoveToRunnerScene(target);

			while (liteNetworkObjects.Count <= index)
			{
				liteNetworkObjects.Add(new LiteNetworkObject(liteNetworkObjects.Count));
			}

			liteNetworkObjects[index].LOCAL_Populate(target.GetComponent<StateBehaviourController>());
		}

		public static bool QueueDeleteLiteObject(LiteNetworkObject networkObject)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority, "BehaviourModifierManager.QueueDeleteLiteObject: HasStateAuthority is false.");
			// UnityEngine.Debug.Assert(self.Runner.Stage != default, "BehaviourModifierManager.QueueDeleteLiteObject: Runner.Stage is not fixed update");

			if (networkObject.QueuedForDestruction)
			{
				return false;
			}

			networkObject.STATE_QueueForDestruction();
			self._deletingLiteObjects.Enqueue(networkObject.Index);
			return true;
		}

		private void LOCAL_ReleaseLiteObject(int targetIndex)
		{
#if DEBUG
			UnityEngine.Debug.Assert(targetIndex >= 0 && targetIndex < liteNetworkObjects.Count, $"BehaviourModifierManager.DestroyTargetObject: targetIndex is out of range: {targetIndex}. Objects: " + liteNetworkObjects.Format());
			UnityEngine.Debug.Assert(liteNetworkObjects[targetIndex].IsReserved, $"BehaviourModifierManager.DestroyTargetObject: targetIndex {targetIndex} is not reserved but trying to release. Objects: " + liteNetworkObjects.Format());
#endif

			GameObject.Destroy(liteNetworkObjects[targetIndex].LOCAL_Free().gameObject);
		}

		public static void RPC_CreateLiteObject(int prefabIndex, TransformData transform)
		{
			self.RPC_CreateLiteObject_(prefabIndex, transform);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_CreateLiteObject_(int prefabIndex, TransformData transform)
		{
			QueueNewNetworkObject(Prefabs[prefabIndex], transform);
		}

		public static void RPC_SetInputAuth(int target, PlayerRef player)
		{
			if (self.liteNetworkObjects[target].IsValid)
				self.liteNetworkObjects[target].SetInputAuthority(player); // Forces immediately locally, then will update
			self.RPC_SetInputAuth_(target, player);
		}

		public static void RPC_UpdateModifier(int address, LiteNetworkedData mod)
		{
			self.RPC_UpdateModifier_(address, mod);
		}

		public static void RPC_AddModifier(LiteNetworkObject networkObject, LiteNetworkedData mod)
		{
			mod.TargetIndex = networkObject.Index;
			mod.PrefabIndex = networkObject.PrefabIndex;
			self.RPC_AddModifier_(mod);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_AddModifier_(LiteDataBuffer mod)
		{
			QueueAddModifier(mod);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_UpdateModifier_(int address, LiteDataBuffer mod)
		{
#if DEBUG
			LiteNetworkedData data = mod;
			UnityEngine.Debug.Assert(data.TargetIndex != -1, "BehaviourModifierManager.RPC_UpdateModifier_: mod.TargetIndex is -1");
			UnityEngine.Debug.Assert(data.PrefabIndex != -1, "BehaviourModifierManager.RPC_UpdateModifier_: mod.PrefabIndex is -1");
			UnityEngine.Debug.Assert(data.ScriptType != -1, "BehaviourModifierManager.RPC_UpdateModifier_: mod.ScriptType is -1");
			LiteNetworkedData oldData = GetModifierData(address);
			UnityEngine.Debug.Assert(oldData.TargetIndex == data.TargetIndex, "BehaviourModifierManager.RPC_UpdateModifier_: oldData.TargetIndex != data.TargetIndex");
			UnityEngine.Debug.Assert(oldData.PrefabIndex == data.PrefabIndex, "BehaviourModifierManager.RPC_UpdateModifier_: oldData.PrefabIndex != data.PrefabIndex");
			UnityEngine.Debug.Assert(oldData.ScriptType == data.ScriptType, "BehaviourModifierManager.RPC_UpdateModifier_: oldData.ScriptType != data.ScriptType");
#endif

			SetModifierData(address, mod);
		}

		// [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		// private void RPC_AddStateMod_(int target, ushort type)
		// {
		// 	StateModifier modifier = LiteNetworkModScripts.StateModifiers[type].CreateModifier();
		// 	if (!TryGetNetworkContext(target, out LiteNetworkObject context))
		// 	{
		// 		Debug.LogError("BehaviourModifierManager.RPC__AddStateMod: target at index " + target + " does not exist.");
		// 		return;
		// 	}

		// 	context.Target.AddStateModifier(modifier);
		// }

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_SetInputAuth_(int target, PlayerRef player)
		{
			SetInputAuthority(target, player);
		}

		private static void ChangedInputAuthorities(Changed<LiteNetworkManager> changed)
		{
			changed.LoadOld();
			foreach (KeyValuePair<ushort, PlayerRef> kvp in changed.Behaviour.InputAuthorities)
			{
				if (kvp.Key < self.liteNetworkObjects.Count && self.liteNetworkObjects[kvp.Key].IsValid)
					self.GetOrPopulateObject(kvp.Key).SetInputAuthority(PlayerRef.None);
			}

			changed.LoadNew();
			foreach (KeyValuePair<ushort, PlayerRef> kvp in changed.Behaviour.InputAuthorities)
			{
				if (kvp.Key < self.liteNetworkObjects.Count && self.liteNetworkObjects[kvp.Key].IsValid)
					self.GetOrPopulateObject(kvp.Key).SetInputAuthority(kvp.Value);
			}
		}

		public static void SetInputAuthority(int target, PlayerRef player)
		{
#if DEBUG
			UnityEngine.Debug.Assert(self.HasStateAuthority, "BehaviourModifierManager.RPC__SetInputAuth: HasStateAuthority is false.");
			UnityEngine.Debug.Assert(self.liteNetworkObjects[target].IsReserved, "BehaviourModifierManager.RPC__SetInputAuth: target at index " + target + " does not exist and not currently trying to clear authority.");
			UnityEngine.Debug.Assert(self.liteNetworkObjects[target].IsValid || player == PlayerRef.None, "BehaviourModifierManager.RPC__SetInputAuth: target at index " + target + " does not exist and not currently trying to clear authority.");
#endif

			if (self.liteNetworkObjects[target].IsValid)
				self.liteNetworkObjects[target].SetInputAuthority(player); // Forces immediately, then will update with the changed event

			if (player == PlayerRef.None)
			{
				self.InputAuthorities.Remove((ushort)target);
				return;
			}

			self.InputAuthorities.Set((ushort)target, player);
		}


		public static void RemoveInputAuthority(int target)
		{
			SetInputAuthority(target, PlayerRef.None);
		}

		// Obsolete. Use NetworkContext.Target.InputAuthority instead.
		// public static PlayerRef GetInputAuth(int target)
		// {
		// 	if (self.InputAuthorities.TryGet((ushort)target, out PlayerRef player))
		// 	{
		// 		return player;
		// 	}

		// 	return PlayerRef.None;
		// }
	}
}