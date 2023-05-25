using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Pnak.Input;
using UnityEngine.InputSystem;

namespace Pnak
{
	public class Interactable : MonoBehaviour
	{
		public delegate void InteractDelegate(Interactable interactable);

		public static List<Interactable> AllInRange = new List<Interactable>();
		public static InteractDelegate OnAnyInteract;
		public static InteractDelegate OnAnyEnterRange;
		public static InteractDelegate OnAnyExitRange;

		public bool IsInteractable = true;
		public float InteractionRadius = 100f;

		public UnityEvent OnEnterRange;
		public UnityEvent OnExitRange;
		public UnityEvent OnInteract;

		private bool inRange = false;
		public bool InRange
		{
			get => inRange;
			set
			{
				if (inRange != value)
				{
					inRange = value;
					if (inRange)
					{
						AllInRange.Add(this);
						OnEnterRange.Invoke();
					}
					else
					{
						AllInRange.Remove(this);
						OnExitRange.Invoke();
					}
				}
			}
		}

		protected void Update()
		{
			if (!IsInteractable || Player.LocalPlayer == null)
			{
				InRange = false;
				return;
			}

			InRange = IsInRange(Player.LocalPlayer.transform.position);
		}
		
		public bool IsInRange(Vector3 position)
		{
			return Vector3.Distance(transform.position, position) <= InteractionRadius;
		}

		private void OnDestroy()
		{
			InRange = false;
		}

		[InputActionTriggered(ActionNames.Interact, InputStateFilters.PreformedThisFrame)]
		private static void OnInteractTriggered(InputAction.CallbackContext context)
		{
			if (AllInRange.Count == 0) return;
			if (Player.LocalPlayer == null) return;
			if (AllInRange.Count == 1)
			{
				AllInRange[0].OnInteract?.Invoke();
				OnAnyInteract?.Invoke(AllInRange[0]);
				return;
			}

			// Get best match by distance and look direction. rank = distance * (1 + degrees/15f)
			Interactable bestMatch = null;
			float bestRank = float.MaxValue;

			Vector3 playerPosition = Player.LocalPlayer.transform.position;
			float lookAngle = GameInput.Instance.InputData.AimAngle;

			foreach (Interactable interactable in AllInRange)
			{
				if (!interactable.IsInteractable) continue;
				if (!interactable.IsInRange(playerPosition)) continue;

				Vector2 difference = interactable.transform.position - playerPosition;
				float angle = MathUtil.DirectionToAngle(difference);
				float distance = difference.magnitude;
				float degrees = Mathf.DeltaAngle(angle, lookAngle);

				float rank = distance * (1 + degrees / 15f);

				if (rank < bestRank)
				{
					bestRank = rank;
					bestMatch = interactable;
				}
			}

			if (bestMatch == null) return;

			bestMatch.OnInteract?.Invoke();
			OnAnyInteract?.Invoke(bestMatch);
		}

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(transform.position, InteractionRadius);

			Gizmos.color = new Color(0, 0, 1, 0.15f);
			Gizmos.DrawSphere(transform.position, InteractionRadius);
		}
#endif
	}
}