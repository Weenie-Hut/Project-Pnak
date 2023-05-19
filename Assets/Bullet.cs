using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Bullet : NetworkBehaviour
	{
		[Networked] private TickTimer life { get; set; }

		[SerializeField] private float speed;
		private Vector3 direction;

		public void Init(float direction)
		{
			life = TickTimer.CreateFromSeconds(Runner, 5.0f);
			this.direction = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction));
		}

		public override void FixedUpdateNetwork()
		{
			if(life.Expired(Runner))
				Runner.Despawn(Object);
			else
			{
				transform.position += speed * direction * Runner.DeltaTime;

				for (int i = 0; i < Enemy.Enemies.Count; i++)
				{
					Enemy enemy = Enemy.Enemies[i];
					if (enemy == null) continue;

					if (Vector3.Distance(transform.position, enemy.transform.position) < 0.1f)
					{
						if (Object.HasInputAuthority)
							Runner.Despawn(Object);
						if (enemy.Damage(1.0f))
							i--;
					}
				}
			}
		}
	}
}
