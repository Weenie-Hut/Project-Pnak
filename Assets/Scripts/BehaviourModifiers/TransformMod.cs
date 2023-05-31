using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public struct TransformData
	{
		public Vector3 Position;
		public Vector2 Scale;
		public float RotationAngle;
	}

	public abstract class TransformMod : LiteNetworkMod
	{
		public abstract void SetTransformData(ref LiteNetworkedData data, TransformData transformData);
	}
}