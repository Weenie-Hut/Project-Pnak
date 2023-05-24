using System;
using System.Collections;

namespace Pnak
{
	public static class CommonCoroutines
	{
		public static IEnumerator CallAfterSeconds(float seconds, Action action)
		{
			yield return YieldPool.GetWaitForSeconds(seconds);
			action?.Invoke();
		}
	}
}