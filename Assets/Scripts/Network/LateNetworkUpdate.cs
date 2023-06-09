using System;
using Fusion;

namespace Pnak
{
	/// <summary>
	/// Helper component to invoke fixed and render update methods before HitboxManager
	/// </summary>
	[OrderAfter(typeof(HitboxManager))]
	public sealed class LateNetworkUpdate : SimulationBehaviour
	{
		public Action OnFixedUpdate = null;
		public Action OnRender = null;

		public override void FixedUpdateNetwork()
		{
			OnFixedUpdate?.Invoke();
		}

		public override void Render()
		{
			OnRender?.Invoke();
		}
	}
}
