using Fusion;
using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(CollisionProcessor))]
	public abstract class Munition : NetworkBehaviour
	{
		public CollisionProcessor CollisionProcessor { get; private set; }

		protected virtual void Awake()
		{
			CollisionProcessor = GetComponent<CollisionProcessor>();
		}

		public override void Spawned()
		{
			base.Spawned();

			CollisionProcessor.OnCollision += OnHit;
		}

		public abstract void Initialize(ModifierContainer modifiers = null);

		protected void Despawn()
		{
			Runner.Despawn(Object);
			RemoveCollisionDetection();
		}

		protected void RemoveCollisionDetection()
		{
			CollisionProcessor.OnCollision -= OnHit;
		}

		protected abstract void OnHit(Collider2D collider2D, float? distance);
	}
}
