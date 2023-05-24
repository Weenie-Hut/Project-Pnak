using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(HealthBehaviour))]
	public class HealthBar : MonoBehaviour
	{
		[SerializeField] private SpriteFillBar _FillBar;

		private void Start()
		{
			var healthBehaviour = GetComponent<HealthBehaviour>();
			healthBehaviour.OnHealthChanged += OnHealthChanged;
			OnHealthChanged(healthBehaviour);
		}

		private void OnHealthChanged(HealthBehaviour healthBehaviour)
		{
			_FillBar.Value = healthBehaviour.Health / healthBehaviour.MaxHealth;
		}
	}
}