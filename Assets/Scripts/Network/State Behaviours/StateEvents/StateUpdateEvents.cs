using UnityEngine;

namespace Pnak
{
	public class StateUpdateEvents : StateBehaviour
	{
		public ScheduledEvent[] Events;

		public override void FixedUpdateNetwork()
		{
			base.FixedUpdateNetwork();

			foreach (var e in Events)
				e.InvokeTrigger(this);
		}
	}
}