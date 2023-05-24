using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(LifetimeBehaviour))]
	public class LifetimeBar : MonoBehaviour
	{
		[SerializeField] private SpriteFillBar _FillBar;
		public LifetimeBehaviour LifetimeBehaviour { get; private set; }

		private void Awake()
		{
			LifetimeBehaviour = GetComponent<LifetimeBehaviour>();
		}

		private void Update()
		{
			_FillBar.Value = LifetimeBehaviour.Lifetime / LifetimeBehaviour.MaxLifetime;
		}
	}
}