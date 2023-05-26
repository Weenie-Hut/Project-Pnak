using Fusion;
using UnityEngine;

namespace Pnak
{
	public class DespawnOnImpact : Munition
	{
		[SerializeField] private LayerMask _DespawnImpactLayers;
		public LayerMask ImpactLayers => _DespawnImpactLayers;

		public override void Initialize(ModifierContainer modifiers)
		{
			// Do nothing. In the future, there might be a modifier that changes the impact layers (like shoot through walls)
		}

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			if ((_DespawnImpactLayers & (1 << collider2D.gameObject.layer)) != 0)
			{
				Despawn();
			}
		}
	}
}