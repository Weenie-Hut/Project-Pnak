using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Pnak
{
	[System.Serializable]
	public struct ScheduledEvent
	{
		[FixedInterval("After/Every {0:F2} seconds", "Instant"), Tooltip("Either the number of ticks after the trigger, or the number of ticks between triggers.")]
		public int Delay;

		[Tooltip("If true, the event will only trigger if the object is still alive after the interval.")]
		[HideIf(nameof(Delay), 0), Tab]
		public bool ActiveIfAlive;
		public UnityEvent Event;

		[System.NonSerialized] public int ticks;

		public void InvokeTrigger(MonoBehaviour owner, bool repeating = false)
		{

			if (repeating && Delay != 0)
			{
				ticks++;

				if (ticks < Delay) return;
				ticks = 0;
			}

			if (Delay == 0)
			{
				Event.Invoke();
			}
			else if (ActiveIfAlive)
			{
				owner.StartCoroutine(InvokeAfterInterval());
			}
			else
			{
				GameManager.Instance.StartCoroutine(InvokeAfterInterval());
			}
		}

		public IEnumerator InvokeAfterInterval()
		{
			yield return YieldPool.GetWaitForSeconds(Delay * Time.fixedDeltaTime);
			Event.Invoke();
		}
	}
}