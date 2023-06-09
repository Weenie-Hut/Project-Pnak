using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct ReloadVisualData
		{
			[HideInInspector]
			public int endTick;
			[Suffix("sec"), HideInInspector]
			public float seconds;
			public Vector3 displayLocalPosition;

			public bool DisplayValid
			{
				get => !(float.IsNaN(displayLocalPosition.x) || float.IsNaN(displayLocalPosition.y) || float.IsNaN(displayLocalPosition.z));
				set => displayLocalPosition = value ? Vector3.zero : new Vector3(float.NaN, float.NaN, float.NaN);
			}
		}


		[FieldOffset(CustomDataOffset)]
		public ReloadVisualData ReloadVisual;
	}

	[CreateAssetMenu(fileName = "ReloadVisual", menuName = "BehaviourModifier/ReloadVisual")]
	public class ReloadVisualMod : LiteNetworkMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.ReloadVisualData);

		public class ReloadVisualContext
		{
			public LiteNetworkObject NetworkContext;
			public FillBar FillBar;
		}

		// TODO: Calculate the position of the bar based on the size of the target (SpriteRenderer.bounds.size.y)?
		// TODO: Pool the fillbar prefabs
		[SerializeField, Required] private FillBar ReloadVisualBarPrefab;
		[SerializeField] private Vector3 defaultDisplayPosition = new Vector3(0, 40, 0);

		public override void SetDefaults(ref LiteNetworkedData data) =>
			SetDefaults(ref data, defaultDisplayPosition);
		public void SetDefaults(ref LiteNetworkedData data, Vector3 displayPosition)
		{
			base.SetDefaults(ref data);

			data.ReloadVisual.seconds = 0.02f;
			data.ReloadVisual.displayLocalPosition = displayPosition;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);

			if (ReloadVisualBarPrefab == null) data.ReloadVisual.DisplayValid = false;
			data.ReloadVisual.endTick = SessionManager.Instance.NetworkRunner.Tick;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			var _context = new ReloadVisualContext { NetworkContext = networkContext };
			context = _context;

			if (!data.ReloadVisual.DisplayValid)
			{
				UnityEngine.Debug.LogWarning("ReloadVisualMod: Target does not have a valid display position. LocalPosition: " + data.ReloadVisual.displayLocalPosition + ". Data: " + data.ToString());
				return;
			}

			if (ReloadVisualBarPrefab == null)
			{
				UnityEngine.Debug.LogWarning("ReloadVisualMod: Target does not have a FillBar prefab but display was enabled. LocalPosition: " + data.ReloadVisual.displayLocalPosition + ". Data: " + data.ToString());
				return;
			}

			_context.FillBar = Instantiate(ReloadVisualBarPrefab.gameObject, networkContext.Target.transform).GetComponent<FillBar>();
			_context.FillBar.RawValueRange.x = 0;
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is ReloadVisualContext ReloadVisualContext)) return;
			if (ReloadVisualContext.FillBar == null)
			{
				UnityEngine.Debug.LogWarning("ReloadVisualMod: context display does not exist! This mod should always have a display so remove if not intended.");
			}
			
			if (!data.ReloadVisual.DisplayValid)
			{
				UnityEngine.Debug.LogWarning("ReloadVisualMod: context display exists but display is false. Destroying display.");
				Destroy(ReloadVisualContext.NetworkContext.Target);
				return;
			}

			ReloadVisualContext.FillBar.transform.localPosition = data.ReloadVisual.displayLocalPosition;

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;
			float secondsLeft = (data.ReloadVisual.endTick - currentTick) * tickRate;

			ReloadVisualContext.FillBar.RawValueRange.y = data.ReloadVisual.seconds;
			ReloadVisualContext.FillBar.RawValue = data.ReloadVisual.seconds - secondsLeft;
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}
	}
}