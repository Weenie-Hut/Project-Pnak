using UnityEngine;

namespace Pnak
{
	public static class ApplicationWantsToQuit
	{
		public static bool IsQuitting { get; private set; }

		private static bool WantsToQuit()
		{
			IsQuitting = true;
			return true;
		}


		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			IsQuitting = false;

			Application.wantsToQuit += WantsToQuit;
		}
	}
}