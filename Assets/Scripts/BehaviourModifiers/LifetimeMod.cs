using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct LifetimeData
		{
			[HideInInspector]
			public int startTick;
			[Suffix("sec")]
			public float seconds;
			public Vector3 displayLocalPosition;

			public bool DisplayValid
			{
				get => !(float.IsNaN(displayLocalPosition.x) || float.IsNaN(displayLocalPosition.y) || float.IsNaN(displayLocalPosition.z));
				set => displayLocalPosition = value ? Vector3.zero : new Vector3(float.NaN, float.NaN, float.NaN);
			}
		}


		[FieldOffset(CustomDataOffset)]
		public LifetimeData Lifetime;
	}

	[CreateAssetMenu(fileName = "Lifetime", menuName = "BehaviourModifier/Lifetime")]
	public class LifetimeMod : LiteNetworkMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.LifetimeData);

		public class LifetimeContext
		{
			public LiteNetworkObject NetworkContext;
			public FillBar FillBar;
		}

		// TODO: Calculate the position of the bar based on the size of the target (SpriteRenderer.bounds.size.y)?
		// TODO: Pool the fillbar prefabs
		[SerializeField, Suffix("Optional")] private FillBar lifetimeBarPrefab;
		[SerializeField, Suffix("sec")] private float defaultSeconds = 5;
		[SerializeField] private Vector3 defaultDisplayPosition = new Vector3(0, 40, 0);

		public override void SetDefaults(ref LiteNetworkedData data) =>
			SetDefaults(ref data, defaultSeconds, defaultDisplayPosition);
		public void SetDefaults(ref LiteNetworkedData data, float seconds) =>
			SetDefaults(ref data, seconds, defaultDisplayPosition);
		public void SetDefaults(ref LiteNetworkedData data, float seconds, Vector3 displayPosition)
		{
			base.SetDefaults(ref data);

			data.Lifetime.seconds = seconds;
			data.Lifetime.displayLocalPosition = displayPosition;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);

			if (lifetimeBarPrefab == null) data.Lifetime.DisplayValid = false;
			data.Lifetime.startTick = SessionManager.Instance.NetworkRunner.Tick;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			var _context = new LifetimeContext { NetworkContext = networkContext };
			context = _context;

			if (!data.Lifetime.DisplayValid) return;

			if (lifetimeBarPrefab == null)
			{
				UnityEngine.Debug.LogWarning("LifetimeMod: Target does not have a FillBar prefab but display was enabled. LocalPosition: " + data.Lifetime.displayLocalPosition + ". Data: " + data.ToString());
				return;
			}

			_context.FillBar = Instantiate(lifetimeBarPrefab.gameObject, networkContext.Target.transform).GetComponent<FillBar>();
			_context.FillBar.RawValueRange.x = 0;
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is LifetimeContext lifetimeContext)) return;

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;
			float endTick = data.Lifetime.startTick + (data.Lifetime.seconds / tickRate);

			if (currentTick >= endTick)
				lifetimeContext.NetworkContext.Target.gameObject.SetActive(false);
			else
				lifetimeContext.NetworkContext.Target.gameObject.SetActive(true);

			if (lifetimeContext.FillBar == null) return;

			if (!data.Lifetime.DisplayValid)
			{
				UnityEngine.Debug.LogWarning("LifetimeMod: context display exists but display is false. Destroying display.");
				Destroy(lifetimeContext.NetworkContext.Target);
				return;
			}

			lifetimeContext.FillBar.transform.localPosition = data.Lifetime.displayLocalPosition;

			lifetimeContext.FillBar.RawValueRange.y = data.Lifetime.seconds;
			lifetimeContext.FillBar.NormalizedValue = 1 - ((currentTick - data.Lifetime.startTick) / (endTick - data.Lifetime.startTick));
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			if (!(rContext is LifetimeContext lifetimeContext)) return;

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;
			float endTick = data.Lifetime.startTick + (data.Lifetime.seconds / tickRate);

			if (currentTick > endTick)
			{
				LiteNetworkManager.QueueDeleteLiteObject(lifetimeContext.NetworkContext);
			}
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is LifetimeContext lifetimeContext)) return;
			if (lifetimeContext.FillBar == null) return;
			Destroy(lifetimeContext.FillBar.gameObject);
		}
	}
}