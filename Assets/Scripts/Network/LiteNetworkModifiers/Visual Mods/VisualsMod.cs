

using System.Runtime.InteropServices;
using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "Visuals", menuName = "BehaviourModifier/Visuals")]
	public class VisualsMod : LiteNetworkMod
	{
		public override System.Type DataType => null;
		
		[Tooltip("This GameObject is only created and destroyed. It must manage its own behaviour such as particle systems or material changes."), Suffix("Optional")]
		public GameObject Visuals;

		public class VisualsContext
		{
			public LiteNetworkObject NetworkContext;
			public GameObject Visual;

			public VisualsContext(LiteNetworkObject networkContext)
			{
				NetworkContext = networkContext;
				Visual = null;
			}
		}

		public override string Format(string format, in LiteNetworkedData data = default)
			=> base.Format(format, data).FormatById("visualName", Visuals.name);

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
			=> context = new VisualsContext(networkContext);

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is VisualsContext visualsContext)) return;

			if (visualsContext.Visual == null && Visuals != null)
				visualsContext.Visual = Instantiate(Visuals, visualsContext.NetworkContext.Target.transform);
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is VisualsContext visualsContext)) return;
			if (visualsContext.Visual != null)
			{
				Destroy(visualsContext.Visual);
				visualsContext.Visual = null;
			}
		}
	}
}