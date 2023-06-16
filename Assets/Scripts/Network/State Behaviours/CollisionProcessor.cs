using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class CollisionProcessor : StateBehaviour
	{
		public enum DistanceCalcType
		{
			[Tooltip("FAST. The distance will be calculated from the transform.position")]
			TransformPosition,
			[Tooltip("FAST. The distance will be calculated from the collider center")]
			ColliderCenter,
			[Tooltip("MED-FAST. The distance will be calculated using smallest distance from the collider bounds.")]
			ClosestPoint_Bounds,
			[Tooltip("MED-FAST. Same as " + nameof(ClosestPoint_Bounds) + ", 0 is returned if the collider is overlapping.")]
			ClosestExternalPoint_Bounds,
			[Tooltip("MED-SLOW. The distance will be calculated using smallest distance from the collider.")]
			ClosestPoint_Collider,
			[Tooltip("SLOW. Same as " + nameof(ClosestPoint_Collider) + ", 0 is returned if the collider is overlapping.")]
			ClosestExternalPoint_Collider
		}

		[Tooltip("The collider that will be used to detect collisions."), Required]
		public Collider2D _Collider;

		[Tooltip("The layers that the collider will collide with.")]
		[SerializeField] private LayerMask _CollisionMask;
		
		[Tooltip("A Rigidbody2D is required on the same object as the collider. If true, the collider will raycast whenever the position is moved in return the colliders hit by the cast. Otherwise (or if no position change), the collider will use overlapping collisions.")]
		[Validate(nameof(ValidateRaycastPositionChanges))]
		public bool RaycastPositionChanges = false;

		[Tooltip("If true, the distances will be calculated for each collision.")]
		public bool CalculateDistances = false;

		[Tooltip("The method used to calculate the distance to the collider.")]
		[ShowIf(nameof(CalculateDistances)), Tab]
		public DistanceCalcType DistanceCalculation = DistanceCalcType.ColliderCenter;

		[Tooltip("The origin of the collider, in local coord of this object. This is used to calculate the distance to the center of the overlapping collider.")]
		[ShowIf(nameof(CalculateDistances)), Tab]
		[Button(nameof(SetOriginToColliderCenter), "Collider Center", "Set the origin to the center of the collider.")]
		[Suffix("local")]
		public Vector2 ColliderOrigin = Vector2.negativeInfinity;

		[Tooltip("Only effects raycasting with distances. If true, the origin will be set relative to the start of the raycast. If false, the origin will be set relative to the current position (end of raycast).")]
		[ShowIf(nameof(CalculateDistances)), Tab]
		[ShowIf(nameof(RaycastPositionChanges))]
		public bool OriginAtRaycastStart = false;

		[Tooltip("If true, the colliders will be sorted by distance from the transform position. If false, the colliders will be sorted by distance from the collider. This can be forced through other components.")]
		[ShowIf(nameof(CalculateDistances)), Tab]
		public bool SortByDistance = false;

		[Tooltip("The maximum number of collisions that can be detected. This is used to create the array of colliders that will be passed to the collision events.")]
		[SerializeField, Min(1)]
		private int maxCollisions = 10;

		public int MaxCollisions => maxCollisions;

		public int ColliderCount { get; private set; }
		public Collider2D[] Colliders { get; private set; }
		public float[] Distances { get; private set; }
		private RaycastHit2D[] RaycastData { get; set; }
		public ContactFilter2D ContactFilter { get; private set; }

		private List<Collider2D> ignoredColliders = new List<Collider2D>();

		private Vector2 lastOrigin = Vector2.zero;
		private Vector2? lastPosition = null;

		protected override void Awake()
		{
			base.Awake();

			Colliders = new Collider2D[maxCollisions];
			if (CalculateDistances) Distances = new float[maxCollisions];
			RaycastData = new RaycastHit2D[maxCollisions];

			ContactFilter = new ContactFilter2D {
				useLayerMask = true,
				layerMask = _CollisionMask,
				useTriggers = false
			};

#if DEBUG
			if (!ValidateRaycastPositionChanges())
			{
				UnityEngine.Debug.LogError($"The collider {name} is set to use raycast position changes, but does not have a Rigidbody2D attached. Disabling raycast position changes.");
				RaycastPositionChanges = false;
			}
#endif
		}

		public void IgnoreCollider(Collider2D collider2D)
		{
			ignoredColliders.Add(collider2D);
		}

		public void ClearIgnoredColliders()
		{
			ignoredColliders.Clear();
		}

		public void ClearIgnoredCollider(Collider2D collider2D)
		{
			ignoredColliders.Remove(collider2D);
		}

		public override void FixedUpdateNetwork()
		{
			Controller.TransformCache.Apply();
			Physics2D.SyncTransforms();

			for (int i = 0; i < ignoredColliders.Count; i++)
			{
				if (ignoredColliders[i] == null)
					ignoredColliders.RemoveAt(i--);
				else ignoredColliders[i].enabled = false;
			}

			// If haven't moved or haven't set previous position
			Vector2 currentPosition = (Vector2)_Collider.transform.position;
			if (!lastPosition.HasValue ||
				Vector2.Distance(lastPosition.Value, currentPosition) < 1f ||
				!RaycastPositionChanges
			) {

				ColliderCount = Physics2D.OverlapCollider(_Collider, ContactFilter, Colliders);
				// UnityEngine.Debug.Log($"Overlapping Calc: {ColliderCount}");

				if (Distances != null)
				{
					lastOrigin = transform.localToWorldMatrix.MultiplyPoint3x4(ColliderOrigin);
					EvaluateDistances(lastOrigin);
				}
			}
			else
			{
				Vector3 direction = currentPosition - lastPosition.Value;
				float distance = direction.magnitude;

				ColliderCount = _Collider.Cast(direction, ContactFilter, RaycastData, distance);
				// UnityEngine.Debug.Log($"Raycast Calc: {ColliderCount}");
				for (int i = 0; i < ColliderCount; i++)
					Colliders[i] = RaycastData[i].collider;

				if (Distances != null)
				{
					Vector2 newOrigin = transform.localToWorldMatrix.MultiplyPoint3x4(ColliderOrigin);
					EvaluateDistances(OriginAtRaycastStart ? lastOrigin : newOrigin);
					lastOrigin = newOrigin;
				}
			}

			lastPosition = currentPosition;

			if (SortByDistance && ColliderCount > 1 && Distances != null)
			{
				Array.Fill(Distances, float.MaxValue, ColliderCount, Distances.Length - ColliderCount);
				Array.Sort(Distances, Colliders);
			}

			for (int i = 0; i < ignoredColliders.Count; i++)
				ignoredColliders[i].enabled = true;
		}

		private void EvaluateDistances(Vector2 point)
		{
			if (Distances == null)
			{
				UnityEngine.Debug.LogWarning($"The collider {name} is trying to calculate distances, but the distances array is null, i.e., the collider is not set to calculate distances.");
				return;
			}

			// UnityEngine.Debug.Log($"Evaluating distances: {DistanceCalculation}, {ColliderCount}");

			switch (DistanceCalculation)
			{
			case DistanceCalcType.TransformPosition:
				for (int i = 0; i < ColliderCount; i++)
					Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].transform.position);
				break;
			case DistanceCalcType.ColliderCenter:
				for (int i = 0; i < ColliderCount; i++)
					Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].bounds.center);
				break;
			case DistanceCalcType.ClosestPoint_Bounds:
				for (int i = 0; i < ColliderCount; i++)
					Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].bounds.ClosestPoint(point));
				break;
			case DistanceCalcType.ClosestExternalPoint_Bounds:
				for (int i = 0; i < ColliderCount; i++)
					if (!Colliders[i].bounds.Contains(point))
						Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].bounds.ClosestPoint(point));
					else Distances[i] = 0f;
				break;
			case DistanceCalcType.ClosestPoint_Collider:
				for (int i = 0; i < ColliderCount; i++)
					Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].ClosestPoint(point));
				break;
			case DistanceCalcType.ClosestExternalPoint_Collider:
				for (int i = 0; i < ColliderCount; i++)
					if (!Colliders[i].OverlapPoint(point))
						Distances[i] = Vector2.Distance(lastOrigin, Colliders[i].ClosestPoint(point));
					else Distances[i] = 0f;
				break;
			}
		}

		public static bool ApplyDamage(Collider2D collider2D, DamageAmount damage)
		{
			IDamageReceiver[] damageReceivers = collider2D.GetComponentsInParent<IDamageReceiver>();

			foreach (IDamageReceiver damageReceiver in damageReceivers)
				damageReceiver.AddDamage(damage);

			return damageReceivers.Length > 0;
		}

		public bool ValidateRaycastPositionChanges()
		{
			return !RaycastPositionChanges || _Collider?.GetComponent<Rigidbody2D>() != null;
		}

		public void SetOriginToColliderCenter()
		{
			ColliderOrigin = transform.worldToLocalMatrix.MultiplyPoint3x4(_Collider.bounds.center);
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (_Collider == null || !CalculateDistances)
				return;

			// Draw the origin
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint3x4(ColliderOrigin), _Collider.bounds.extents.magnitude * 0.05f);
		}

		private void OnValidate()
		{
			if (_Collider == null) return;

			if (float.IsNegativeInfinity(ColliderOrigin.x) || float.IsNegativeInfinity(ColliderOrigin.y))
				SetOriginToColliderCenter();
		}
#endif
	}
}