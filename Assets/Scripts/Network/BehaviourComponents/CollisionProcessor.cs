using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class CollisionProcessor : StateBehaviour
	{
		[Tooltip("The collider that will be used to detect collisions.")]
		[SerializeField] private Collider2D _Collider;
		[Tooltip("The layers that the collider will collide with.")]
		[SerializeField] private LayerMask _CollisionMask;
		[Tooltip("If true, the colliders will be sorted by distance from the transform position. If false, the colliders will be sorted by distance from the collider. This can be forced through other components.")]
		public bool SortByDistance = false;
		[Tooltip("If SortByDistance is true, this will be used to calculate the distance between the colliders. If true, the distance between the colliders will be calculated using the transform position. If false, the distance between the colliders will be calculated using the collider distance. False is more accurate, but more expensive.")]
		[SerializeField] private bool UseTransformDistance = false;
		[Tooltip("The maximum number of collisions that can be detected. This is used to create the array of colliders that will be passed to the collision events.")]
		[Min(1)] public int maxCollisions = 10;

		public int ColliderCount { get; private set; }
		public Collider2D[] Colliders { get; private set; }
		public float[] Distances { get; private set; }

		private List<Collider2D> ignoredColliders = new List<Collider2D>();

		protected override void Awake()
		{
			base.Awake();

			Colliders = new Collider2D[maxCollisions];
			if (SortByDistance) Distances = new float[maxCollisions];
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
			for (int i = 0; i < ignoredColliders.Count; i++)
			{
				if (ignoredColliders[i] == null)
					ignoredColliders.RemoveAt(i--);
				else ignoredColliders[i].enabled = false;
			}

			ColliderCount = Physics2D.OverlapCollider(_Collider, new ContactFilter2D {
				useLayerMask = true,
				layerMask = _CollisionMask,
				useTriggers = false
			}, Colliders);

			for (int i = 0; i < ignoredColliders.Count; i++)
				ignoredColliders[i].enabled = true;

			if (ColliderCount < 0)
				return;

			if (Distances != null && ColliderCount > 1)
			{
				for (int i = 0; i < ColliderCount; i++)
					if (UseTransformDistance)
						Distances[i] = Vector2.Distance(_Collider.transform.position, Colliders[i].transform.position);
					else
						Distances[i] = Colliders[i].Distance(_Collider).distance;

				Array.Fill(Distances, float.MaxValue, ColliderCount, Distances.Length - ColliderCount);
				Array.Sort(Distances, Colliders);
			}
		}

		public static bool ApplyDamage(Collider2D collider2D, DamageAmount damage)
		{
			IDamageReceiver[] damageReceivers = collider2D.GetComponentsInParent<IDamageReceiver>();

			foreach (IDamageReceiver damageReceiver in damageReceivers)
				damageReceiver.AddDamage(damage);

			return damageReceivers.Length > 0;
		}
	}
}