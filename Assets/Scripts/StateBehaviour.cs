using UnityEngine;
using Fusion;

namespace Pnak
{
	[RequireComponent(typeof(StateBehaviourController))]
	public abstract class StateBehaviour : MonoBehaviour
	{
		protected NetworkRunner Runner => SessionManager.Instance.NetworkRunner;
		private StateBehaviourController _controller;
		public StateBehaviourController Controller
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

		private void OnEnable()
		{
			if (SessionManager.IsServer)
			{
				Enabled();
			}
		}

		protected virtual void Enabled()
		{
		}

		private void OnDisable()
		{
			if (SessionManager.IsServer)
			{
				Disabled();
			}
		}

		protected virtual void Disabled()
		{
		}

		public virtual void FixedUpdateNetwork()
		{
		}

		public virtual void OnDataCreated(LiteNetworkedData[] mods, ref TransformData transform)
		{
		}

		public virtual void Initialize()
		{
		}

		public virtual void Render()
		{
		}
	}
}