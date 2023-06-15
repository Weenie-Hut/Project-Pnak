using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public struct TransformData : INetworkStruct
	{
		public Vector3 Position;
		public Vector2 Scale;
		public float RotationAngle;

		public override string ToString()
		{
			return $"{{p: {Position}, a: {RotationAngle}, s: {Scale}}}";
		}
	}

	public abstract class TransformMod : LiteNetworkMod
	{
		public abstract void SetTransformData(ref LiteNetworkedData data, TransformData transformData);
		public virtual void UpdateTransform(ref LiteNetworkedData data, TransformData transformData)
			=> SetTransformData(ref data, transformData);
		public abstract TransformData GetTransformData(object context, in LiteNetworkedData data);

		public TransformData GetTransformData(int modifierAddress)
			=> GetTransformData(LiteNetworkManager.GetModContext(modifierAddress), LiteNetworkManager.GetModifierData(modifierAddress));
	}
}