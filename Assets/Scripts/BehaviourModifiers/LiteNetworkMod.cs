using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public struct SerializedLiteNetworkedData
	{
		public byte scriptType;
		public unsafe fixed byte CustomData[LiteNetworkedData.CustomDataSize];

		public LiteNetworkedData ToLiteNetworkedData()
		{
			LiteNetworkedData data = new LiteNetworkedData();
			data.ScriptType = scriptType - 1;
			unsafe {
				fixed (byte* ptr = CustomData) {
					for (int i = 0; i < LiteNetworkedData.CustomDataSize; i++) {
						data.CustomData[i] = ptr[i];
					}
				}
			}
			return data;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public partial struct LiteNetworkedData
	{
		public const int CustomDataOffset = 4;
		public const int CustomDataSize = 28;

		public int ScriptType
		{
			get => scriptType - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < byte.MaxValue - 1, "BehaviourModifierData.ScriptType: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (byte.MaxValue - 1) + "], Actual value: " + value.ToString());
				scriptType = (byte)(value + 1);
			}
		}

		public int TargetIndex
		{
			get => targetIndex - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < ushort.MaxValue - 1, "BehaviourModifierData.LocalTargetIndex: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (ushort.MaxValue - 1) + "], Actual value: " + value.ToString());
				targetIndex = (ushort)(value + 1);
			}
		}

		public int PrefabIndex
		{
			get => prefabIndex - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < byte.MaxValue - 1, "BehaviourModifierData.PrefabIndex: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (byte.MaxValue - 1) + "], Actual value: " + value.ToString());
				prefabIndex = (byte)(value + 1);
			}
		}

		public bool IsValid => prefabIndex != 0;

		public void InvalidateScript() => targetIndex = scriptType = 0;
		public void Invalidate() => prefabIndex = 0;

		public void Remove()
		{
			scriptType = 0;
		}

		public static LiteNetworkedData Create(int targetIndex, int prefabIndex)
		{
			LiteNetworkedData result = default;
			result.TargetIndex = targetIndex;
			result.PrefabIndex = prefabIndex;

			return result;
		}

		[FieldOffset(0)]
		private byte scriptType;
		[FieldOffset(1)]
		private byte prefabIndex;
		[FieldOffset(2)]
		private ushort targetIndex;

		// Custom data
		[FieldOffset(4)]
		public unsafe fixed byte CustomData[CustomDataSize];

		public byte[] SafeCustomData
		{
			get {
				byte[] result = new byte[CustomDataSize];
				unsafe {
					fixed (byte* ptr = result) {
						for (int i = 0; i < CustomDataSize; i++) {
							ptr[i] = CustomData[i];
						}
					}
				}

				return result;
			}
		}

		public override string ToString()
		{
			return "{ScriptType: " + ScriptType + ", PrefabIndex: " + PrefabIndex + ", TargetIndex: " + TargetIndex + " Data: " + this.ToBytes().HexString() + "}";
		}
	}

	public abstract class LiteNetworkMod : ScriptableObject
	{
		public abstract System.Type DataType { get; }

		[SerializeField, ReadOnly] private int scriptIndex = -1;
		public int ScriptIndex => scriptIndex;

#if UNITY_EDITOR
		public void EditorSetScriptIndex(int index) => scriptIndex = index;
#endif

		public virtual void SetDefaults(ref LiteNetworkedData data)
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
#endif
				data.ScriptType = ScriptIndex;
			// SetRuntime(ref data);
		}

		public virtual void SetRuntime(ref LiteNetworkedData data)
		{
		}

		public virtual void Initialize(LiteNetworkObject target, in LiteNetworkedData data, out object context)
		{
			context = target;
		}
		
		public virtual void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
		}
		
		public virtual void OnInvalidatedUpdate(object rContext, ref LiteNetworkedData data)
		{
			data.Invalidate();
		}

		public virtual void OnRender(object context, in LiteNetworkedData data)
		{
		}

		public virtual void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
		}

		
	}
}