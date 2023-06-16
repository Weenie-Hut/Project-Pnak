using UnityEngine;

namespace Pnak
{
	public class StateInitializeEvents : StateBehaviour
	{
		public ScheduledEvent[] Events;

		public override void FixedInitialize()
		{
			base.FixedInitialize();

			foreach (var e in Events)
				e.InvokeTrigger(this);
		}
	}
}