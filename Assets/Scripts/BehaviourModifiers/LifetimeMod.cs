using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		public struct LifetimeData : INetworkStruct
		{
			public int startTick;
			public int endTick;
			public float displayPosition;
		}


		[FieldOffset(CustomDataOffset)]
		public LifetimeData Lifetime;
	}

	[CreateAssetMenu(fileName = "Lifetime", menuName = "BehaviourModifier/Lifetime")]
	public class LifetimeMod : LiteNetworkMod
	{
		// TODO: Calculate the position of the bar based on the size of the target (SpriteRenderer.bounds.size.y)?
		// TODO: Pool the fillbar prefabs
		[SerializeField] private FillBar lifetimeBarPrefab;
		[SerializeField] private float defaultSeconds = 5;
		[SerializeField] private float defaultDisplayPosition = 35f;

		public LiteNetworkedData CreateData(float seconds = float.NaN, float displayPosition = float.NaN)
		{
			if (float.IsNaN(seconds)) seconds = defaultSeconds;
			if (float.IsNaN(displayPosition)) displayPosition = defaultDisplayPosition;

			LiteNetworkedData result = default;
			result.ScriptType = ScriptIndex;
			result.Lifetime.startTick = SessionManager.Instance.NetworkRunner.Tick;
			result.Lifetime.endTick = result.Lifetime.startTick + (int)(seconds / SessionManager.Instance.NetworkRunner.DeltaTime);
			result.Lifetime.displayPosition = displayPosition;

			return result;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			base.Initialize(networkContext, data, out context); // Set context if early return

			if (data.Lifetime.displayPosition == float.NaN) return;

			if (lifetimeBarPrefab == null)
			{
				UnityEngine.Debug.LogWarning("LifetimeMod: Target does not have a FillBar prefab but display was enabled.");
				return;
			}

			context = Instantiate(lifetimeBarPrefab.gameObject, networkContext.Target.transform).GetComponent<FillBar>();
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			if (data.Lifetime.displayPosition == float.NaN)
			{
				UnityEngine.Debug.LogWarning("LifetimeMod: context display exists but display is false. Destroying display.");
				Destroy(fillBar.gameObject);
				return;
			}

			fillBar.transform.localPosition = new Vector3(0, data.Lifetime.displayPosition, 0);

			float currentTick = SessionManager.Instance.NetworkRunner.Tick;
			float tickRate = SessionManager.Instance.NetworkRunner.DeltaTime;

			fillBar.RawValueRange.x = 0;
			// End is seconds between start and end ticks
			fillBar.RawValueRange.y = (data.Lifetime.endTick - data.Lifetime.startTick) * tickRate;
			fillBar.NormalizedValue = (currentTick - data.Lifetime.startTick) / (data.Lifetime.endTick - data.Lifetime.startTick);
		}

		public override void OnFixedUpdate(object rContext, ref LiteNetworkedData data)
		{
			float currentTick = SessionManager.Instance.NetworkRunner.Tick;

			if (currentTick > data.Lifetime.endTick)
			{
				LiteNetworkManager.QueueDeleteLiteObject(data.TargetIndex);
			}
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}
	}
}