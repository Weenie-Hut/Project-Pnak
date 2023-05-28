using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	[StructLayout(LayoutKind.Explicit)]
	public partial struct BehaviourModifierData : INetworkStruct
	{
		public const int CustomDataOffset = 4;

		public int ScriptType
		{
			get => scriptType - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < sizeof(byte) - 1, "BehaviourModifierData.ScriptType: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (sizeof(byte) - 1) + "], Actual value: " + value.ToString());
				scriptType = (byte)(value + 1);
			}
		}

		public int TargetIndex
		{
			get => targetIndex - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < sizeof(ushort) - 1, "BehaviourModifierData.LocalTargetIndex: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (sizeof(ushort) - 1) + "], Actual value: " + value.ToString());
				targetIndex = (ushort)(value + 1);
			}
		}

		public int PrefabIndex
		{
			get => prefabIndex - 1;
			set {
				System.Diagnostics.Debug.Assert(value >= 0 && value < sizeof(byte) - 1, "BehaviourModifierData.PrefabIndex: value is out of range. This is either a bug or the size needs to be increased. Expected range: [0, " + (sizeof(byte) - 1) + "], Actual value: " + value.ToString());
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


		[FieldOffset(0)]
		private byte scriptType;
		[FieldOffset(1)]
		private ushort targetIndex;
		[FieldOffset(3)]
		private byte prefabIndex;

		// Custom data
	}

	public abstract class BehaviourModifier : ScriptableObject
	{
		protected static BehaviourModifierData GetModifierData(int index) => BehaviourModifierManager.Instance.GetModifierData(index);
		protected static void SetModifierData(int index, in BehaviourModifierData data) => BehaviourModifierManager.Instance.SetModifierData(index, data);

		private int scriptIndex = -1;
		public int ScriptIndex => scriptIndex == -1 ?
			scriptIndex = BehaviourModifierManager.Instance.GetScriptIndex(this) :
			scriptIndex;

		public virtual void Initialize(NetworkObjectContext target, in BehaviourModifierData data, out object context)
		{
			context = target;
		}
		
		public virtual void OnFixedUpdate(object rContext, ref BehaviourModifierData data)
		{
		}
		
		public virtual void OnInvalidatedUpdate(object rContext, ref BehaviourModifierData data)
		{
			data.Invalidate();
		}

		public virtual void OnRender(object context, in BehaviourModifierData data)
		{
		}

		public virtual void OnInvalidatedRender(object context, in BehaviourModifierData data)
		{
		}

		
	}
}