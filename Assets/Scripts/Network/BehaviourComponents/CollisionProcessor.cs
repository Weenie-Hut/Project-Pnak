using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class CollisionProcessor : NetworkBehaviour
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

		public Action<Collider2D, float?> OnCollision;
		public Action<Collider2D[], float[]> OnCollisions;

		private Collider2D[] colliders;
		private float[] distances;

		private List<Collider2D> ignoredColliders = new List<Collider2D>();

		public override void Spawned()
		{
			base.Spawned();

			colliders = new Collider2D[maxCollisions];
			if (SortByDistance) distances = new float[maxCollisions];
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
			if (OnCollision == null && OnCollisions == null)
				return;

			for (int i = 0; i < ignoredColliders.Count; i++)
			{
				if (ignoredColliders[i] == null)
					ignoredColliders.RemoveAt(i--);
				else ignoredColliders[i].enabled = false;
			}

			int count = Physics2D.OverlapCollider(_Collider, new ContactFilter2D {
				useLayerMask = true,
				layerMask = _CollisionMask,
				useTriggers = false
			}, colliders);

			for (int i = 0; i < ignoredColliders.Count; i++)
				ignoredColliders[i].enabled = true;

			if (count < 0)
				return;

			if (distances != null && count > 1)
			{
				for (int i = 0; i < count; i++)
					if (UseTransformDistance)
						distances[i] = Vector2.Distance(_Collider.transform.position, colliders[i].transform.position);
					else
						distances[i] = colliders[i].Distance(_Collider).distance;

				Array.Fill(distances, float.MaxValue, count, distances.Length - count);
				Array.Sort(distances, colliders);
			}

			for (int i = 0; i < count; i++)
			{
				OnCollision?.Invoke(colliders[i], distances?[i]);
			}

			OnCollisions?.Invoke(colliders, distances);
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