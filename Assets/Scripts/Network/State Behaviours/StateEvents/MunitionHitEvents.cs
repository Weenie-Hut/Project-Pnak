using UnityEngine;

namespace Pnak
{
	public class MunitionHitEvents : Munition
	{
		public ScheduledEvent[] Events;

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			base.OnHit(collider2D, distance);

			foreach (var e in Events) e.InvokeTrigger(this);
		}
	}
}