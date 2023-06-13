using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	/// <summary>
	/// Static class to cache yield objects and provide easy access to them from coroutines.
	/// </summary>
	public static class YieldPool
	{
		public static WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
		public static WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

		private static SortedDictionary<int , WaitForSeconds> WaitForSecondsCache = new SortedDictionary<int, WaitForSeconds>();
		private static SortedDictionary<int, WaitForSecondsRealtime> WaitForSecondsCacheRealtime = new SortedDictionary<int, WaitForSecondsRealtime>();

		public static WaitForSeconds GetWaitForSeconds(float seconds)
		{
			var roundedSeconds = (int)(seconds * 100.0f);
			WaitForSeconds waitForSeconds;
			if (!WaitForSecondsCache.TryGetValue(roundedSeconds, out waitForSeconds))
			{
				if (Time.time > 60f) Debug.Log("Creating WaitForSeconds: " + (roundedSeconds / 100.0f));
				waitForSeconds = new WaitForSeconds(roundedSeconds / 100.0f);
				WaitForSecondsCache.Add(roundedSeconds, waitForSeconds);
			}

			return waitForSeconds;
		}

		public static WaitForSecondsRealtime GetWaitForSecondsRealtime(float seconds)
		{
			var roundedSeconds = (int)(seconds * 100.0f);

			WaitForSecondsRealtime waitForSeconds;
			if (!WaitForSecondsCacheRealtime.TryGetValue(roundedSeconds, out waitForSeconds))
			{
				if (Time.time > 60f) Debug.Log("Creating WaitForSecondsRealtime: " + (roundedSeconds / 100.0f));
				waitForSeconds = new WaitForSecondsRealtime(roundedSeconds / 100.0f);
				WaitForSecondsCacheRealtime.Add(roundedSeconds, waitForSeconds);
			}

			return waitForSeconds;
		}
	}
}