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
		public static StateModifierSO[] StateModifiers => self.LiteNetworkModScripts.StateModifiers;
		public static StateBehaviourController[] Prefabs => self.LiteNetworkModScripts.LiteNetworkPrefabs;

		[Networked, Capacity(LiteModCapacity)]
		private NetworkArray<LiteDataBuffer> LiteModData { get; }

		[Networked, Capacity(10)]
		private NetworkDictionary<ushort, PlayerRef> InputAuthorities { get; }

		/// <summary>
		/// The highest index in ModifiersData that is currently in use.
		/// Used as a hint to avoid iterating over the entire array (performance).
		/// </summary>
		[Networked] private int liteModUsingCapacity { get; set; }
		// TODO: This is to make sure that if an index is disabled and enabled with different data, we can clear the data first.
		private LiteNetworkedData[] liteModDataCopy;

		// private ReferencePool<GameObject>[] _pseudoNetworkPrefabPools;

		private object[] liteModContexts;
		public static object GetModContext(int index) => self.liteModContexts[index];

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

			LateUpdate.OnFixedUpdate += LateFixedNetworkUpdate;

			// _pseudoNetworkPrefabPools = new ReferencePool<GameObject>[LiteNetworkPrefabs.Length];
			// for (int i = 0; i < LiteNetworkPrefabs.Length; i++)
			// 	_pseudoNetworkPrefabPools[i] = new GameObjectPool(LiteNetworkPrefabs[i]);

			liteModDataCopy = new LiteNetworkedData[LiteModCapacity];
		}

		public static LiteNetworkedData GetModifierData(int index)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority);
			return self.LiteModData[index];
		}

		public static void SetModifierData(int index, in LiteNetworkedData data)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority);
			self.LiteModData.Set(index, data);
		}

		public static LiteNetworkObject GetNetworkObject(int index)
		{
			if (!self.HasStateAuthority)
			{
				UnityEngine.Debug.LogError("GetNetworkObject is not safe on client nodes. Maybe make sure that objects are initialized on clients before this can be called?");
				return null;
			}

			return self.liteNetworkObjects[index];
		}

		public static TransformData? TryGetTransformData(int objIndex)
			=> TryGetTransformData(GetNetworkObject(objIndex));

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

		public static int CreateRawNetworkObjectContext() => self.ReserveFreeNetworkIndex();
		public static int CreateRawNetworkObjectContext(out LiteNetworkedData data, StateBehaviourController prefab)
		{
			int target = LiteNetworkManager.CreateRawNetworkObjectContext();
			data = default;
			data.TargetIndex = target;
			data.PrefabIndex = prefab.PrefabIndex;

			return target;
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

		public void RunQueueCreate()
		{
			while (_createNetworkObjectQueue.Count > 0)
			{
				CreateNetworkObjectData data = _createNetworkObjectQueue.Dequeue();
				int target = self.ReserveFreeNetworkIndex();
				AddAndInitDefaultModifiers(target, data);
				
				data.Clear();
				_createNetworkObjectDataPool.Return(data);
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
			_workingModAddresses.Clear();
			int lastModifierAddress = -1;

			for (int i = 0; i < modifiersData.Length; i++)
			{
				modifiersData[i].TargetIndex = targetIndex;
				modifiersData[i].PrefabIndex = data.Prefab.PrefabIndex;

				LiteNetworkMod script = ModScripts[modifiersData[i].ScriptType];

				script.SetRuntime(ref modifiersData[i]);

				if (script is TransformMod transformModifier)
					transformModifier.SetTransformData(ref modifiersData[i], data.Transform);

				lastModifierAddress = self.ReserveFreeModifierIndex(modifiersData[i].ScriptType, modifiersData[i].TargetIndex, lastModifierAddress);
				PlaceModifier(lastModifierAddress, in modifiersData[i]);
				self.InitializeContext(lastModifierAddress);

				_workingModAddresses.Add(lastModifierAddress);
			}

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

			liteNetworkObjects.Add(new LiteNetworkObject(liteNetworkObjects.Count));
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
				UnityEngine.Debug.Assert(HasStateAuthority);
				UnityEngine.Debug.Assert(data.ScriptType < ModScripts.Length, "BehaviourModifierManager.ReplaceModifier: data.scriptType is out of range");
				UnityEngine.Debug.Assert(data.PrefabIndex < Prefabs.Length, "BehaviourModifierManager.ReplaceModifier: data.prefabIndex is out of range");
				UnityEngine.Debug.Assert(data.TargetIndex < LiteModCapacity, "BehaviourModifierManager.ReplaceModifier: parameter data.targetIndex is out of range: Range is [0, " + LiteModCapacity + "), but data.targetIndex is " + data.TargetIndex);
				UnityEngine.Debug.Assert(modifierAddress != -1, "BehaviourModifierManager.AddModifier: ModifierCapacity (" + LiteModCapacity + ") reached. Modifier will not be added.");
				// UnityEngine.Debug.Log("BehaviourModifierManager.PlaceModifier: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

				// UnityEngine.Debug.Log($"Placing at {modifierAddress}: {data},\n Target {liteNetworkObjects[data.TargetIndex].Format()},\nContext: {liteModContexts[modifierAddress]}\n");

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

		public void LateFixedNetworkUpdate()
		{
			if (!HasStateAuthority) return;

			// Recursively calls itself until queue is empty. Makes sure the all objects are created and ran in correct order.
			RunQueueCreate();

			while (_deletingLiteObjects.Count > 0)
			{
				int targetIndex = _deletingLiteObjects.Dequeue();

				for (int objectModifierIndex = 0; objectModifierIndex < liteNetworkObjects[targetIndex].Modifiers.Count; objectModifierIndex++)
				{
					int modifierAddress = liteNetworkObjects[targetIndex].Modifiers[objectModifierIndex];

					LiteNetworkedData data = LiteModData[modifierAddress];
					if (!data.IsValid) continue;
					if (data.TargetIndex != targetIndex) continue;

					ModScripts[data.ScriptType].OnInvalidatedUpdate(liteModContexts[modifierAddress], ref data);
					LiteModData.Set(modifierAddress, data);

					// UnityEngine.Debug.Log("BehaviourModifierManager.Invalidate (update) Modifier due to delete: { address: " + modifierAddress + ", scriptType: " + data.ScriptType + ", prefabIndex: " + data.PrefabIndex + ", targetIndex: " + data.TargetIndex + " }");

					// If the object was invalidated the same frame that it was initialized, only the state will have context
					if (!liteModDataCopy[modifierAddress].IsValid)
					{
						liteNetworkObjects[data.TargetIndex].Modifiers.RemoveAt(objectModifierIndex);
						ModScripts[data.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in data);
						liteModContexts[modifierAddress] = null;

						objectModifierIndex--;
					}
				}

				RemoveInputAuthority(targetIndex);
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

			for (int modifierAddress = 0; modifierAddress < liteModUsingCapacity; modifierAddress++)
			{
				LiteNetworkedData data = LiteModData[modifierAddress];
				if (!data.IsValid) continue;

				if (liteModContexts[modifierAddress] == null)
				{
					UnityEngine.Debug.LogError("BehaviourModifierManager.FixedUpdateNetwork: liteModContexts[" + modifierAddress + "] is null");
					InitializeContext(modifierAddress);
				}

				ModScripts[data.ScriptType].OnFixedUpdate(liteModContexts[modifierAddress], ref data);
				LiteModData.Set(modifierAddress, data);
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
							liteNetworkObjects[previous.TargetIndex].Target != null && // If target still exits
							liteNetworkObjects[previous.TargetIndex].Modifiers.Remove(modifierAddress)) // If target still has this modifier
						{
							ModScripts[previous.ScriptType].OnInvalidatedRender(liteModContexts[modifierAddress], in previous);
							liteModContexts[modifierAddress] = null;
						}
					}
				}
				else if (!HasStateAuthority && liteModContexts[modifierAddress] != null)
				{
					UnityEngine.Debug.LogError($"BehaviourModifierManager.Render: liteModContexts[{modifierAddress}] != null -> Previous: {previous.ToString()} ;;;;; Current: {current.ToString()} ;;;;; Context: {liteModContexts[modifierAddress].ToString()}");
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
					if (current.TargetIndex != targetForAddress)
					{
						var target = liteNetworkObjects[targetForAddress];
						UnityEngine.Debug.LogWarning($"Object {targetForAddress} which contains modifier address {modifierAddress} ({target.Modifiers.Format()}) does not match the current data target -> Previous: {previous.ToString()} ;;;;; Current: {current.ToString()}");
					}
				}

				liteModDataCopy[modifierAddress] = current;
			}

			// Note that the objects are returned before executing the normal render because if a modifier was removed and replace with another on the same update, the new modifier could possibly target the same object index but be intended for a different object
			for (int i = 0; i < liteNetworkObjects.Count; i++)
			{
				// UnityEngine.Debug.Log($"Checking {i}: {liteNetworkObjects[i].IsReserved} && {liteNetworkObjects[i].Target == null} && {liteNetworkObjects[i].Modifiers.Count} > 0 ;;;;; {liteNetworkObjects[i].Modifiers.Format()}");
				if (liteNetworkObjects[i].Target == null) continue;
				if (liteNetworkObjects[i].Modifiers.Count > 0) continue;
				UnityEngine.Debug.Assert(!HasStateAuthority || liteNetworkObjects[i].IsReserved);

				ReleaseLiteObject(i);
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

				try {
					ModScripts[data.ScriptType].OnRender(liteModContexts[modifierAddress], in data);
				}
				catch (System.Exception e)
				{
					UnityEngine.Debug.LogError(data.ToString() + " :: " + liteModContexts[modifierAddress].ToString());
					UnityEngine.Debug.LogError($"BehaviourModifierManager.Render: {e.Message}\n{e.StackTrace}");
				}
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
			GameObject target = GameObject.Instantiate(Prefabs[prefab].gameObject);
			Runner.MoveToRunnerScene(target);

			while (liteNetworkObjects.Count <= index)
				liteNetworkObjects.Add(new LiteNetworkObject(liteNetworkObjects.Count));

			liteNetworkObjects[index].Target = target.GetComponent<StateBehaviourController>();
			liteNetworkObjects[index].PrefabIndex = prefab;

			context = liteNetworkObjects[index];
		}

		public static bool IsQueuedForDeletion(int index) => self._deletingLiteObjects.Contains(index);

		public static bool QueueDeleteLiteObject(int index)
		{
			UnityEngine.Debug.Assert(self.HasStateAuthority, "BehaviourModifierManager.QueueDeleteLiteObject: HasStateAuthority is false.");
			// UnityEngine.Debug.Assert(self.Runner.Stage != default, "BehaviourModifierManager.QueueDeleteLiteObject: Runner.Stage is not fixed update");

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
			UnityEngine.Debug.Assert(context.Target != null, "BehaviourModifierManager.DestroyTargetObject: context.Target is null.");
			UnityEngine.Debug.Assert(context.Modifiers.Count == 0, "BehaviourModifierManager.DestroyTargetObject: context.Modifiers.Count > 0.");

			GameObject.Destroy(context.Target.gameObject);
			// context.Target.SetActive(false);
			// _pseudoNetworkPrefabPools[context.PrefabIndex].Return(context.Target);
			context.Target = null;
			context.PrefabIndex = -1;
			context.IsReserved = false;
		}

		public static void RPC_AddStateMod(int target, ushort type)
		{
			self.RPC_AddStateMod_(target, type);
		}

		public static void RPC_SetInputAuth(int target, PlayerRef player)
		{
			self.RPC_SetInputAuth_(target, player);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_AddStateMod_(int target, ushort type)
		{
			StateModifier modifier = LiteNetworkModScripts.StateModifiers[type].CreateModifier();
			if (!TryGetNetworkContext(target, out LiteNetworkObject context))
			{
				Debug.LogError("BehaviourModifierManager.RPC__AddStateMod: target at index " + target + " does not exist.");
				return;
			}

			context.Target.AddStateModifier(modifier);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_SetInputAuth_(int target, PlayerRef player)
		{
			SetInputAuthority(target, player);
		}

		public static void SetInputAuthority(int target, PlayerRef player)
		{
			if (!TryGetNetworkContext(target, out LiteNetworkObject context))
			{
				Debug.LogError("BehaviourModifierManager.RPC__SetInputAuth: target at index " + target + " does not exist.");
				return;
			}

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

		public static PlayerRef GetInputAuth(int target)
		{
			if (self.InputAuthorities.TryGet((ushort)target, out PlayerRef player))
			{
				return player;
			}

			return PlayerRef.None;
		}
	}
}