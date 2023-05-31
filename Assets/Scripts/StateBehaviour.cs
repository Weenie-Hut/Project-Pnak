using UnityEngine;
using Fusion;

namespace Pnak
{
	[RequireComponent(typeof(StateBehaviourController))]
	public abstract class StateBehaviour : MonoBehaviour
	{
		protected NetworkRunner Runner => SessionManager.Instance.NetworkRunner;
		private StateBehaviourController _controller;
		protected StateBehaviourController Controller
		{
			get {
				if (_controller == null)
					_controller = GetComponent<StateBehaviourController>();
				return _controller;
			}
			private set => _controller = value;
		}

		protected virtual void Awake()
		{
		}

		public virtual void FixedUpdateNetwork()
		{
		}

		public virtual void OnDataCreated(LiteNetworkedData[] mods, ref TransformData transform)
		{
		}
	}
}