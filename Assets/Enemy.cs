using Fusion;

namespace Pnak
{
	public class Enemy : NetworkBehaviour
	{
		[Networked] private TickTimer life { get; set; }

		public void Init()
		{
			life = TickTimer.CreateFromSeconds(Runner, 5.0f);
		}

		public override void FixedUpdateNetwork()
		{
			if(life.Expired(Runner))
				Runner.Despawn(Object);
			else
				transform.position += 5 * transform.forward * Runner.DeltaTime;
		}
	}
}
