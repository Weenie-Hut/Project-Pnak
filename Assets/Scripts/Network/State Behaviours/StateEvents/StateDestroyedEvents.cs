using UnityEngine;

namespace Pnak
{
	public class StateDestroyedEvents : StateBehaviour
	{
		public ScheduledEvent[] Events;

		public override void QueuedForDestroy()
		{
			base.QueuedForDestroy();

			foreach (var e in Events)
				e.InvokeTrigger(this);
		}
	}
}