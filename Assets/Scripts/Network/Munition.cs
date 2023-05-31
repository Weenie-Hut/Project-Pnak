using Fusion;
using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(CollisionProcessor))]
	public abstract class Munition : StateBehaviour
	{
		public CollisionProcessor CollisionProcessor { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			CollisionProcessor = GetComponent<CollisionProcessor>();
		}

		public override void FixedUpdateNetwork()
		{
			if (CollisionProcessor.ColliderCount == 0) return;

			for (int i = 0; i < CollisionProcessor.ColliderCount && !Controller.QueuedForDestroy; i++)
			{
				OnHit(CollisionProcessor.Colliders[i], CollisionProcessor.Distances?[i]);
			}
		}

		protected abstract void OnHit(Collider2D collider2D, float? distance);
	}
}
