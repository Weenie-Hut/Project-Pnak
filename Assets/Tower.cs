using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Tower : NetworkBehaviour
	{
		[Networked] private TickTimer reloadTime { get; set; }
		[Networked] private TickTimer life { get; set; }

		[SerializeField] private Bullet _BulletPrefab;
		[Tooltip("How fast in degrees the tower rotates towards enemies")]
		[SerializeField] private float _DefaultInitialDelay = 1f;
		[SerializeField] private float _DefaultRotationSpeed = 3f;
		[SerializeField] private float _DefaultReloadDelay = 1f;
		[SerializeField] private float _DefaultLifeTime = 10f;
		[SerializeField] private Transform _GunTransform;

		[SerializeField] private SpriteFillBar _ReloadBar;
		[SerializeField] private SpriteFillBar _LifeBar;

		private float rotationSpeed;
		private float reloadDelay;
		private float lifeTime;

		[Networked] private float Rotation { get; set; }

		public void Init(float _reloadDelay, float lifeTime, float _rotation)
		{
			rotationSpeed = _DefaultRotationSpeed;
			reloadDelay = _reloadDelay;
			float initialDelay = Mathf.Min(_DefaultInitialDelay, reloadDelay);
			reloadTime = TickTimer.CreateFromSeconds(Runner, initialDelay);
			this.lifeTime = lifeTime;
			life = TickTimer.CreateFromSeconds(Runner, lifeTime);
			Rotation = _rotation;
		}

		private void Update()
		{
			float? temp;
			if ((temp = reloadTime.RemainingTime(Runner)).HasValue)
				_ReloadBar.Value = 1 - temp.Value / reloadDelay;

			if ((temp = life.RemainingTime(Runner)).HasValue)
				_LifeBar.Value = temp.Value / lifeTime;

			_GunTransform.rotation = Quaternion.Euler(0, 0, Rotation);
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

					float delta = Mathf.DeltaAngle(Rotation, angle);
					Rotation = Rotation + Mathf.Clamp(delta, -rotationSpeed, rotationSpeed);
				}

				// Shoot
				if (reloadTime.ExpiredOrNotRunning(Runner))
				{
					Runner.Spawn(_BulletPrefab, transform.position, Quaternion.Euler(0, 0, Rotation), Object.InputAuthority, (runner, o) =>
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
