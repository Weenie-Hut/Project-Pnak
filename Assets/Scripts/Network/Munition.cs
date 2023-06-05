using Fusion;
using UnityEngine;

namespace Pnak
{
	public abstract class Munition : StateBehaviour
	{
		[Attached, SerializeField] private CollisionProcessor _collisionProcessor;
		public CollisionProcessor CollisionProcessor => _collisionProcessor;

		public override void FixedUpdateNetwork()
		{
			if (CollisionProcessor.ColliderCount == 0) return;

			for (int i = 0; i < CollisionProcessor.ColliderCount && !Controller.QueuedForDestroy; i++)
			{
				if (CollisionProcessor.Colliders[i] == null) continue;
				OnHit(CollisionProcessor.Colliders[i], CollisionProcessor.Distances?[i]);
			}
		}

		protected virtual void OnHit(Collider2D collider2D, float? distance)
		{
			UnityEngine.Debug.LogWarning($"Munition {name} ({GetType().Name}) should either override OnHit(Collider2D, float?) or override FixedUpdateNetwork()");
		}
	}
}
