using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class RaycastScaleBehaviour : StateBehaviour
	{
		[Flags]
		public enum Axis
		{
			X = 1 << 0,
			Y = 1 << 1,
		}

		[Tooltip("The layer mask to raycast for.")]
		[SerializeField] private LayerMask HitLayerMask;
		[Tooltip("The maximum distance in units to raycast for. If raycast misses, the scale will be set to 'MaxUnits * ScalePerUnit'.")]
		[SerializeField] private float MaxUnits;
		[Tooltip("The scale per unit of distance. Ex: 0.1 scale per unit with 100 units will result in a scale of 10.")]
		[SerializeField] private float ScalePerUnit;
		[Tooltip("The axis to scale on. Raycast will always be cast to the right (positive x).")]
		[SerializeField] private Axis ScaleAxis;
		// [SerializeField] private bool Update = false;

		public override void OnDataCreated(LiteNetworkedData[] mods, ref TransformData transform)
		{
			base.OnDataCreated(mods, ref transform);
			CastForScale(ref transform);
		}

		private void CastForScale(ref TransformData transform)
		{
			float scale;

			RaycastHit2D hit = Physics2D.Raycast(transform.Position, MathUtil.AngleToDirection(transform.RotationAngle), MaxUnits, HitLayerMask);
			if (hit.collider != null)
			{
				scale = hit.distance * ScalePerUnit;
			}
			else
			{
				scale = MaxUnits * ScalePerUnit;
			}

			if (ScaleAxis.HasFlag(Axis.X))
				transform.Scale.x = scale;

			if (ScaleAxis.HasFlag(Axis.Y))
				transform.Scale.y = scale;
		}
	}
}