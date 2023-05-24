using Fusion;

namespace Pnak
{
	public static class NetworkExtensions
	{
		public static void DespawnSelf(this NetworkBehaviour networkBehaviour)
		{
			networkBehaviour.Runner.Despawn(networkBehaviour.Object);
		}
	}

	public interface IDamageReceiver
	{
		bool AddDamage(DamageAmount damage);
	}
}