using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class CollisionProcessor : StateBehaviour
	{
		[Tooltip("The collider that will be used to detect collisions."), Required]
		[SerializeField] private Collider2D _Collider;

		[Tooltip("The layers that the collider will collide with.")]
		[SerializeField] private LayerMask _CollisionMask;
		
		[Tooltip("A Rigidbody2D is required on the same object as the collider. If true, the collider will raycast whenever the position is moved in return the colliders hit by the cast. Otherwise (or if no position change), the collider will use overlapping collisions.")]
		[Validate(nameof(ValidateRaycastPositionChanges))]
		public bool RaycastPositionChanges = false;

		[Tooltip("If true, the colliders will be sorted by distance from the transform position. If false, the colliders will be sorted by distance from the collider. This can be forced through other components.")]
		public bool SortByDistance = false;

		public bool UseDistanceOnStaticTransform;

		[Tooltip("If SortByDistance is true, this will be used to calculate the distance between the colliders. If true, the distance between the colliders will be calculated using the transform position. If false, the distance between the colliders will be calculated using the collider distance. False is more accurate, but more expensive.")]
		[SerializeField, ShowIf(nameof(UseDistanceOnStaticTransform))]
		private bool UseTransformDistanceOnStaticTransform = false;

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
		private Vector2 lastPosition = Vector2.negativeInfinity;

		protected override void Awake()
		{
			base.Awake();

			Colliders = new Collider2D[maxCollisions];
			Distances = new float[maxCollisions];
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
			Physics2D.SyncTransforms();

			for (int i = 0; i < ignoredColliders.Count; i++)
			{
				if (ignoredColliders[i] == null)
					ignoredColliders.RemoveAt(i--);
				else ignoredColliders[i].enabled = false;
			}

			// If haven't moved or haven't set previous position
			Vector2 currentPosition = (Vector2)_Collider.transform.position;
			if (Vector2.Distance(lastPosition, currentPosition) < 1f ||
				(float.IsNegativeInfinity(lastPosition.x) || float.IsNegativeInfinity(lastPosition.y)) ||
				!RaycastPositionChanges
			) {
				// UnityEngine.Debug.Log($"Overlapping");

				ColliderCount = Physics2D.OverlapCollider(_Collider, ContactFilter, Colliders);

				for (int i = 0; i < ColliderCount && UseDistanceOnStaticTransform; i++)
					if (UseTransformDistanceOnStaticTransform)
						Distances[i] = Vector2.Distance(_Collider.transform.position, Colliders[i].transform.position);
					else
						Distances[i] = Colliders[i].Distance(_Collider).distance;
			}
			else
			{
				Vector3 direction = currentPosition - lastPosition;
				float distance = direction.magnitude;


				ColliderCount = _Collider.Cast(direction, ContactFilter, RaycastData, distance);
				for (int i = 0; i < ColliderCount; i++)
					Colliders[i] = RaycastData[i].collider;
				for (int i = ColliderCount; i < RaycastData.Length; i++)
					Distances[i] = RaycastData[i].distance;

				// UnityEngine.Debug.Log($"Direction: {direction}, Distance: {distance} ({ColliderCount}) -> {Colliders.Format()}");
			}

			lastPosition = currentPosition;

			if (SortByDistance && ColliderCount > 1)
			{
				Array.Fill(Distances, float.MaxValue, ColliderCount, Distances.Length - ColliderCount);
				Array.Sort(Distances, Colliders);
			}

			for (int i = 0; i < ignoredColliders.Count; i++)
				ignoredColliders[i].enabled = true;
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
	}
}