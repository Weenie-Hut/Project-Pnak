using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(LifetimeBehaviour))]
	public class LifetimeBar : MonoBehaviour
	{
		[SerializeField] private FillBar _FillBar;
		public LifetimeBehaviour LifetimeBehaviour { get; private set; }

		private void Awake()
		{
			LifetimeBehaviour = GetComponent<LifetimeBehaviour>();
		}

		private void Start()
		{
			_FillBar.RawValueRange.x = 0;
		}

		private void Update()
		{
			_FillBar.RawValueRange.y = LifetimeBehaviour.MaxLifetime;
			_FillBar.NormalizedValue = LifetimeBehaviour.Lifetime / LifetimeBehaviour.MaxLifetime;
		}
	}
}