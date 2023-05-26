using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(HealthBehaviour))]
	public class HealthBar : MonoBehaviour
	{
		[SerializeField] private FillBar _FillBar;

		private void Start()
		{
			var healthBehaviour = GetComponent<HealthBehaviour>();
			healthBehaviour.OnHealthChanged += OnHealthChanged;
			OnHealthChanged(healthBehaviour);
			_FillBar.RawValueRange.x = 0;
		}

		private void OnHealthChanged(HealthBehaviour healthBehaviour)
		{
			_FillBar.RawValueRange.y = healthBehaviour.MaxHealth;
			_FillBar.NormalizedValue = healthBehaviour.Health / healthBehaviour.MaxHealth;
		}
	}
}