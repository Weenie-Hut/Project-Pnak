using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Tower : NetworkBehaviour
	{
		[Networked] private TickTimer reloadTime { get; set; }

		[SerializeField] private Projectile _BulletPrefab;
		[SerializeField] private float _SpawnReloadDelay = 1f;
		[SerializeField] private float _SpawnInitialReloadDelay = 1f;
		[Tooltip("How fast in degrees the tower rotates towards enemies")]
		[SerializeField] private float _SpawnRotationSpeed = 3f;
		[SerializeField] private Transform _GunTransform;
		[SerializeField] private LayerMask _TargetMask;
		[SerializeField] private SpriteFillBar _ReloadBar;

		private float rotationSpeed;
		private float reloadDelay;

		[Networked] private float Rotation { get; set; }

		public override void Spawned()
		{
			base.Spawned();

			rotationSpeed = _SpawnRotationSpeed;
			reloadDelay = _SpawnReloadDelay;
			float initialDelay = _SpawnInitialReloadDelay;
			reloadTime = TickTimer.CreateFromSeconds(Runner, initialDelay);
		}

		public void Init(float rotation)
		{
			Rotation = rotation;
		}

		private void Update()
		{
			float? temp;
			if ((temp = reloadTime.RemainingTime(Runner)).HasValue)
				_ReloadBar.Value = 1 - temp.Value / reloadDelay;

			_GunTransform.rotation = Quaternion.Euler(0, 0, Rotation);
		}

		public override void FixedUpdateNetwork()
		{
			// Find closest enemy
			Transform closestEnemy = GetClosestTarget();

			// Rotate towards closest enemy
			if (closestEnemy == null) return;

			Vector3 direction = closestEnemy.transform.position - transform.position;
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

			float delta = Mathf.DeltaAngle(Rotation, angle);
			float rotationSpeed = this.rotationSpeed * Runner.DeltaTime;
			Rotation = Rotation + Mathf.Clamp(delta, -rotationSpeed, rotationSpeed);

			// If the angle if greater than 30 degrees, don't shoot
			if (Mathf.Abs(Mathf.DeltaAngle(Rotation, angle)) > 30f) return;

			// Shoot
			if (reloadTime.ExpiredOrNotRunning(Runner))
			{
				Runner.Spawn(_BulletPrefab, transform.position, Quaternion.Euler(0, 0, Rotation), Object.InputAuthority);

				reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
			}
		}

		private Collider2D[] colliders = new Collider2D[20];
		private float[] distances = new float[20];

		private Transform GetClosestTarget()
		{
			int count = Physics2D.OverlapCircle(transform.position, 100f, new ContactFilter2D {
				useLayerMask = true,
				layerMask = _TargetMask
			}, colliders);

			for (int i = 0; i < 20; i++)
			{
				if (colliders[i] == null) distances[i] = float.MaxValue;
				else distances[i] = Vector3.Distance(transform.position, colliders[i].transform.position);
			}

			Array.Sort(distances, colliders);

			if (count < 0 || colliders[0] == null)
				return null;

			return colliders[0].transform;
		}
	}
}
