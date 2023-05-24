using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class RaycastScaleBehaviour : NetworkBehaviour
	{
		[SerializeField] private LayerMask HitLayerMask;
		[SerializeField] private float MaxUnits;
		[SerializeField] private float ScalePerUnit;
		[SerializeField] private bool Update = false;

		public override void Spawned()
		{
			base.Spawned();

			CastForScale();
		}

		public override void FixedUpdateNetwork()
		{
			if (Update)
			{
				CastForScale();
			}
		}

		private void CastForScale()
		{
			RaycastHit2D hit = Physics2D.Raycast(transform.position, MathUtil.AngleToDirection(transform.localEulerAngles.z), MaxUnits, HitLayerMask);
			if (hit.collider != null)
			{
				transform.localScale = new Vector3(hit.distance * ScalePerUnit, transform.localScale.y, transform.localScale.z);
			}
			else
			{
				transform.localScale = new Vector3(MaxUnits * ScalePerUnit, transform.localScale.y, transform.localScale.z);
			}
		}
	}
}