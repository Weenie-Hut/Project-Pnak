using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Bullet : NetworkBehaviour
	{
		[Networked] private TickTimer life { get; set; }

		[SerializeField] private float speed;
		private Vector3 direction;

		public void Init()
		{
			life = TickTimer.CreateFromSeconds(Runner, 5.0f);
			float angle = transform.localEulerAngles.z;
			direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0.0f);
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

					if (Vector3.Distance(transform.position, enemy.transform.position) < 0.6f)
					{
						Runner.Despawn(Object);
						if (enemy.Damage(1.0f))
							i--;
					}
				}
			}
		}
	}
}
