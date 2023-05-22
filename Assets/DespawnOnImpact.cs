using Fusion;
using UnityEngine;

namespace Pnak
{
	public class DespawnOnImpact : Projectile
	{
		[SerializeField] private LayerMask _DespawnImpactLayers;

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			if ((_DespawnImpactLayers & (1 << collider2D.gameObject.layer)) != 0)
			{
				Despawn();
			}
		}
	}
}