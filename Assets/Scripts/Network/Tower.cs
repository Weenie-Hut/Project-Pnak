using System;
using Fusion;
using Pnak.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class Tower : NetworkBehaviour
	{
// 		[Networked] private TickTimer reloadTime { get; set; }

// 		[SerializeField] private Munition _BulletPrefab;
// 		[SerializeField] private Collider2D TargetArea;
// 		[SerializeField] private LayerMask _LOSBlockMask;
// 		[SerializeField] private float _SpawnReloadDelay = 1f;
// 		[SerializeField] private float _SpawnInitialReloadDelay = 1f;
// 		[Tooltip("How fast in degrees the tower rotates towards enemies")]
// 		[SerializeField] private float _SpawnRotationSpeed = 3f;
// 		[SerializeField] private Transform _GunTransform;
// 		[SerializeField] private LayerMask _TargetMask;
// 		[SerializeField] private SpriteFillBar _ReloadBar;
// 		[SerializeField] private int MaxCollisionChecks = 10;
// 		[SerializeField] private RadialOptionSO[] InteractionOptions;


// 		[Networked] private float rotationSpeed { get; set; }
// 		[Networked(OnChanged=nameof(OnReloadChanged))] private float reloadDelay { get; set; }

// 		[Networked] private float Rotation { get; set; }

// 		private void Awake()
// 		{
// 			_ReloadBar.RawValueRange.x = 0f;
// 		}

// 		public override void Spawned()
// 		{
// 			base.Spawned();

// 			rotationSpeed = _SpawnRotationSpeed;
// 			reloadDelay = _SpawnReloadDelay;
// 			float initialDelay = _SpawnInitialReloadDelay;
// 			reloadTime = TickTimer.CreateFromSeconds(Runner, initialDelay);

// 			colliders = new Collider2D[MaxCollisionChecks];
// 			distances = new float[MaxCollisionChecks];

// 			if (Object.HasStateAuthority)
// 			{
// 			}
// 		}

// 		public override void Despawned(NetworkRunner runner, bool hasState)
// 		{
// 			if (Object.HasInputAuthority)
// 				Player.LocalPlayer.RPC_UnsetPilot(Object.Id);
// 		}

// 		[InputActionTriggered(ActionNames.Interact)]
// 		public void OnInteract(InputAction.CallbackContext context)
// 		{
// 			if (Object.HasInputAuthority)
// 				Player.LocalPlayer.RPC_UnsetPilot(Object.Id);
// 		}

// 		public void Init(float rotation)
// 		{
// 			Rotation = rotation;
// 		}

// 		private void Update()
// 		{
// 			float? temp;
// 			if ((temp = reloadTime.RemainingTime(Runner)).HasValue)
// 				_ReloadBar.NormalizedValue = 1 - temp.Value / reloadDelay;

// 			_GunTransform.rotation = Quaternion.Euler(0, 0, Rotation);
// 		}

// 		public override void FixedUpdateNetwork()
// 		{
// 			if (!HasStateAuthority) return; // TODO: Predictive spawning for bullets

// 			if (GetInput(out NetworkInputData input))
// 			{
// 				float targetRotation = input.AimAngle;
// 				float deltaAngle = Mathf.DeltaAngle(Rotation, targetRotation);
// 				float _rotationSpeed = this.rotationSpeed * Runner.DeltaTime;
// 				Rotation = Rotation + Mathf.Clamp(deltaAngle, -_rotationSpeed, _rotationSpeed);

// 				if (reloadTime.ExpiredOrNotRunning(Runner))
// 				{
// 					if (input.GetButtonPressed(1))
// 					{
// 						LiteNetworkManager.CreateNetworkObjectContext(_BulletPrefab, new TransformData {
// 							Position = transform.position, RotationAngle = Rotation
// 						});
// 						reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
// 					}
// 				}

// 				return;
// 			}

// 			// Find closest enemy
// 			Transform closestEnemy = GetClosestTarget();

// 			// Rotate towards closest enemy
// 			if (closestEnemy == null) return;

// 			Vector3 direction = closestEnemy.transform.position - transform.position;
// 			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

// 			float delta = Mathf.DeltaAngle(Rotation, angle);
// 			float rotationSpeed = this.rotationSpeed * Runner.DeltaTime;
// 			Rotation = Rotation + Mathf.Clamp(delta, -rotationSpeed, rotationSpeed);

// 			// If the angle if greater than 30 degrees, don't shoot
// 			if (Mathf.Abs(Mathf.DeltaAngle(Rotation, angle)) > 30f) return;

// 			// Shoot
// 			if (reloadTime.ExpiredOrNotRunning(Runner))
// 			{
// 				LiteNetworkManager.CreateNetworkObjectContext(_BulletPrefab, new TransformData {
// 					Position = transform.position, RotationAngle = Rotation
// 				});
// 				reloadTime = TickTimer.CreateFromSeconds(Runner, reloadDelay);
// 			}
// 		}

// 		private static void OnReloadChanged(Changed<Tower> changed)
// 		{
// 			changed.Behaviour._ReloadBar.RawValueRange.y = changed.Behaviour.reloadDelay;
// 		}

// 		private Collider2D[] colliders;
// 		private float[] distances;

// 		private Transform GetClosestTarget()
// 		{
// 			int count = Physics2D.OverlapCollider(TargetArea, new ContactFilter2D {
// 				useLayerMask = true,
// 				layerMask = _TargetMask
// 			}, colliders);

// 			if (count < 0 || colliders[0] == null)
// 				return null;

// 			for (int i = 0; i < MaxCollisionChecks; i++)
// 			{
// 				if (colliders[i] == null) distances[i] = float.MaxValue;
// 				else distances[i] = Vector3.Distance(transform.position, colliders[i].transform.position);
// 			}

// 			Array.Sort(distances, colliders);

// 			for (int i = 0; i < MaxCollisionChecks; i++)
// 			{
// 				if (colliders[i] == null) continue;

// 				if (_LOSBlockMask != 0)
// 				{
// 					// If there is a collier on any LOSBlockMask layer from the line between the tower and the target, skip this target
// 					RaycastHit2D hit = Physics2D.Linecast(transform.position, colliders[i].transform.position, _LOSBlockMask);
// 					if (hit.collider != null) continue;
// 				}

// 				return colliders[i].transform;
// 			}

// 			return null;
// 		}

// #if UNITY_EDITOR
// 		[Header("Editor")]
// 		[SerializeField] private bool _InheritLOSFromBullet = true;

// 		public void OnValidate()
// 		{
// 			if (_InheritLOSFromBullet && _BulletPrefab != null)
// 			{
// 				DespawnOnImpact[] impacts = _BulletPrefab.GetComponentsInChildren<DespawnOnImpact>();

// 				for (int i = 0; i < impacts.Length; i++)
// 				{
// 					_LOSBlockMask |= impacts[i].ImpactLayers;
// 				}
// 			}
// 		}
// #endif
	}
}
