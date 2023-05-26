using System;
using Fusion;
using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(ModifierContainer))]
	public class Tower : NetworkBehaviour
	{
		[Networked] private TickTimer reloadTime { get; set; }

		[SerializeField] private Projectile _BulletPrefab;
		[SerializeField] private Collider2D TargetArea;
		[SerializeField] private LayerMask _LOSBlockMask;
		[SerializeField] private float _SpawnReloadDelay = 1f;
		[SerializeField] private float _SpawnInitialReloadDelay = 1f;
		[Tooltip("How fast in degrees the tower rotates towards enemies")]
		[SerializeField] private float _SpawnRotationSpeed = 3f;
		[SerializeField] private Transform _GunTransform;
		[SerializeField] private LayerMask _TargetMask;
		[SerializeField] private SpriteFillBar _ReloadBar;
		[SerializeField] private int MaxCollisionChecks = 10;
		[SerializeField] private RadialOptionSO[] InteractionOptions;

		public ModifierContainer ModifierContainer { get; private set; }

		[Networked] private float rotationSpeed { get; set; }
		[Networked(OnChanged=nameof(OnReloadChanged))] private float reloadDelay { get; set; }

		[Networked] private float Rotation { get; set; }

		private void Awake()
		{
			_ReloadBar.RawValueRange.x = 0f;
		}

		public override void Spawned()
		{
			base.Spawned();

			rotationSpeed = _SpawnRotationSpeed;
			reloadDelay = _SpawnReloadDelay;
			float initialDelay = _SpawnInitialReloadDelay;
			reloadTime = TickTimer.CreateFromSeconds(Runner, initialDelay);

			colliders = new Collider2D[MaxCollisionChecks];
			distances = new float[MaxCollisionChecks];

			if (Object.HasStateAuthority)
			{
				ModifierContainer = GetComponent<ModifierContainer>();
				ModifierContainer.OnModifierAdded += OnModifierAdded;
				ModifierContainer.OnModifierRemoved += OnModifierRemoved;
			}

			Interactable.OnAnyInteract += OnAnyInteract;
		}

		private void OnDestroy()
		{
			Interactable.OnAnyInteract -= OnAnyInteract;
		}


		private void OnAnyInteract(Interactable interactable)
		{
			if (interactable.gameObject != gameObject) return;

			RadialMenu.Instance.Show(InteractionOptions, interactable);
		}

		private void OnModifierAdded(Modifier modifier)
		{
			if (modifier.type == ModifierTarget.Reload)
			{
				float previous = reloadDelay;

				reloadDelay = modifier.ApplyValue(reloadDelay);
				
				float? remainingTime = reloadTime.RemainingTime(Runner);

				if (remainingTime.HasValue)
				{
					// If the reload time is less than the previous reload time, then we need to adjust the timer
					if (reloadDelay < previous)
					{
						reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay * remainingTime.Value / previous);
					}
				}
			}
		}

		private void OnModifierRemoved(Modifier modifier)
		{
			if (modifier.type == ModifierTarget.Reload)
			{
				reloadDelay = modifier.RemoveValue(reloadDelay);
			}
		}

		public void Init(float rotation)
		{
			Rotation = rotation;
		}

		private void Update()
		{
			float? temp;
			if ((temp = reloadTime.RemainingTime(Runner)).HasValue)
				_ReloadBar.NormalizedValue = 1 - temp.Value / reloadDelay;

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
				Runner.Spawn(_BulletPrefab, transform.position, Quaternion.Euler(0, 0, Rotation), null, (_, bullet) =>
				{
					bullet.GetComponent<Projectile>().Initialize(ModifierContainer);
				});

				reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
			}
		}

		private static void OnReloadChanged(Changed<Tower> changed)
		{
			changed.Behaviour._ReloadBar.RawValueRange.y = changed.Behaviour.reloadDelay;
		}

		private Collider2D[] colliders;
		private float[] distances;

		private Transform GetClosestTarget()
		{
			int count = Physics2D.OverlapCollider(TargetArea, new ContactFilter2D {
				useLayerMask = true,
				layerMask = _TargetMask
			}, colliders);

			if (count < 0 || colliders[0] == null)
				return null;

			for (int i = 0; i < MaxCollisionChecks; i++)
			{
				if (colliders[i] == null) distances[i] = float.MaxValue;
				else distances[i] = Vector3.Distance(transform.position, colliders[i].transform.position);
			}

			Array.Sort(distances, colliders);

			for (int i = 0; i < MaxCollisionChecks; i++)
			{
				if (colliders[i] == null) continue;

				if (_LOSBlockMask != 0)
				{
					// If there is a collier on any LOSBlockMask layer from the line between the tower and the target, skip this target
					RaycastHit2D hit = Physics2D.Linecast(transform.position, colliders[i].transform.position, _LOSBlockMask);
					if (hit.collider != null) continue;
				}

				return colliders[i].transform;
			}

			return null;
		}

#if UNITY_EDITOR
		[Header("Editor")]
		[SerializeField] private bool _InheritLOSFromBullet = true;

		public void OnValidate()
		{
			if (_InheritLOSFromBullet && _BulletPrefab != null)
			{
				DespawnOnImpact[] impacts = _BulletPrefab.GetComponentsInChildren<DespawnOnImpact>();

				for (int i = 0; i < impacts.Length; i++)
				{
					_LOSBlockMask |= impacts[i].ImpactLayers;
				}
			}
		}
#endif
	}
}
