

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
	public class DurationAndVisualsMod : VisualsMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.DurationAndVisualsData);

		public override string Format(string format, in LiteNetworkedData data = default)
			=> base.Format(format, data).FormatById("visualName", Visuals.name);

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);
			data.DurationAndVisuals.startTick = SessionManager.Tick;
		}

		protected float DurationRemaining(in LiteNetworkedData data)
			=> data.DurationAndVisuals.Duration - (SessionManager.Tick - data.DurationAndVisuals.startTick) * SessionManager.DeltaTime;

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			base.OnFixedUpdate(rContext, ref data);

			if (SessionManager.HasExpired(data.DurationAndVisuals.startTick, data.DurationAndVisuals.Duration))
			{
				data.Invalidate();
			}
		}
	}
}