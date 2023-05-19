using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Tower : NetworkBehaviour
	{
		[Networked] private TickTimer reloadTime { get; set; }
		[Networked] private TickTimer life { get; set; }

		[SerializeField] private Bullet _BulletPrefab;
		[SerializeField] private float _RotationSpeed;

		private float reloadDelay;

		public void Init(float reloadDelay, float lifeTime)
		{
			this.reloadDelay = reloadDelay;
			reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
			life = TickTimer.CreateFromSeconds(Runner, lifeTime);
		}

		public override void FixedUpdateNetwork()
		{
			if(life.Expired(Runner))
				Runner.Despawn(Object);
			else
			{
				// Find closest enemy
				Enemy closestEnemy = null;
				float closestDistance = float.MaxValue;
				for (int i = 0; i < Enemy.Enemies.Count; i++)
				{
					Enemy enemy = Enemy.Enemies[i];
					if (enemy == null) continue;

					float distance = Vector3.Distance(transform.position, enemy.transform.position);
					if (distance < closestDistance)
					{
						closestEnemy = enemy;
						closestDistance = distance;
					}
				}

				// Rotate towards closest enemy
				if (closestEnemy != null)
				{
					Vector3 direction = closestEnemy.transform.position - transform.position;
					float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
					transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0.0f, 0.0f, angle), _RotationSpeed * Runner.DeltaTime);
				}

				// Shoot
				if (reloadTime.ExpiredOrNotRunning(Runner))
				{
					Runner.Spawn(_BulletPrefab, transform.position, transform.rotation, Object.InputAuthority, (runner, o) =>
					{
						Bullet bullet = o.GetComponent<Bullet>();
						bullet.Init();
					});

					reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
				}
			}
		}
	}
}
