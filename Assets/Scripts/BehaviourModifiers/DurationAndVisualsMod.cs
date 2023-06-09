

using System.Runtime.InteropServices;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct DurationAndVisualsData
		{
			[Suffix("sec"), Tooltip("The duration of this upgrade. Use 0 or less for permanent upgrades.")]
			[Default(float.PositiveInfinity), Min(0)]
			public float Duration;
			[HideInInspector]
			public int startTick;
		}


		[FieldOffset(CustomDataOffset)]
		public DurationAndVisualsData DurationAndVisuals;
	}

	[CreateAssetMenu(fileName = "DurationAndVisuals", menuName = "BehaviourModifier/DurationAndVisuals")]
	public class DurationAndVisualsMod : LiteNetworkMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.DurationAndVisualsData);
		
		[Tooltip("This GameObject is only created and destroyed. It must manage its own behaviour such as particle systems or material changes."), Suffix("Optional")]
		public GameObject Visuals;

		[Suffix("sec"), Tooltip("The duration of this upgrade. Use 0 or less for permanent upgrades.")]
			[Default(float.PositiveInfinity), Min(0)]
		public float defaultDuration = 5f;

		public class DurationAndVisualsContext
		{
			public LiteNetworkObject NetworkContext;
			public GameObject Visual;

			public DurationAndVisualsContext(LiteNetworkObject networkContext)
			{
				NetworkContext = networkContext;
				Visual = null;
			}
		}

		public virtual string Format(string format, in LiteNetworkedData data = default)
			=> format.FormatById("visualName", Visuals.name);

		public override void SetDefaults(ref LiteNetworkedData data)
		{
			base.SetDefaults(ref data);
			data.DurationAndVisuals.Duration = defaultDuration;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);
			data.DurationAndVisuals.startTick = SessionManager.Instance.NetworkRunner.Tick;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
			=> context = new DurationAndVisualsContext(networkContext);

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (SessionManager.HasExpired(data.DurationAndVisuals.startTick, data.DurationAndVisuals.Duration))
			{
				data.Invalidate();
			}
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is DurationAndVisualsContext DurationAndVisualsContext)) return;

			if (DurationAndVisualsContext.Visual == null && Visuals != null)
				DurationAndVisualsContext.Visual = Instantiate(Visuals, DurationAndVisualsContext.NetworkContext.Target.transform);
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is DurationAndVisualsContext DurationAndVisualsContext)) return;
			if (DurationAndVisualsContext.Visual != null)
			{
				Destroy(DurationAndVisualsContext.Visual);
				DurationAndVisualsContext.Visual = null;
			}
		}
	}
}