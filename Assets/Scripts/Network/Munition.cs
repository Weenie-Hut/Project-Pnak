using Fusion;
using UnityEngine;

namespace Pnak
{
	public abstract class Munition : StateBehaviour
	{
		[Attached, SerializeField] private CollisionProcessor _collisionProcessor;
		public CollisionProcessor CollisionProcessor => _collisionProcessor;

		[Validate(nameof(ValidateHitsPerTick)), Min(1), Tooltip("The maximum number of hits that is processed per tick. Use for munitions that effect an area but only apply to a limited number of targets.")]
		public int MaxHitsPerTick = 1;

		protected int workingHitsRemaining;
		public override void FixedUpdateNetwork()
		{
			if (CollisionProcessor.ColliderCount == 0) return;

			workingHitsRemaining = MaxHitsPerTick;

			for (int i = 0; i < CollisionProcessor.ColliderCount && !Controller.NetworkContext.QueuedForDestruction && workingHitsRemaining > 0; i++)
			{
				if (CollisionProcessor.Colliders[i] == null || !CollisionProcessor.Colliders[i].enabled) continue;
				OnHit(CollisionProcessor.Colliders[i], CollisionProcessor.Distances?[i]);
				workingHitsRemaining--;
			}
		}

		protected virtual void OnHit(Collider2D collider2D, float? distance)
		{
			UnityEngine.Debug.LogWarning($"Munition {name} ({GetType().Name}) should either override OnHit(Collider2D, float?) or override FixedUpdateNetwork()");
		}

		public bool ValidateHitsPerTick()
		{
			return MaxHitsPerTick <= (CollisionProcessor?.MaxCollisions ?? 0);
		}

		private void Reset()
		{
			MaxHitsPerTick = GetComponent<CollisionProcessor>()?.MaxCollisions ?? 1;
		}

		protected virtual void OnValidate()
		{
			// MaxHitsPerTick = GetComponent<CollisionProcessor>()?.MaxCollisions ?? 1;
		}
	}
}
