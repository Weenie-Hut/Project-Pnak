using System.Runtime.InteropServices;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public partial struct LiteNetworkedData
	{
		[System.Serializable]
		public struct HealthVisualData
		{
			[HideInInspector]
			public float maxHealth;
			[HideInInspector]
			public float currentHealth;
			public Vector3 displayLocalPosition;

			public bool DisplayValid
			{
				get => !(float.IsNaN(displayLocalPosition.x) || float.IsNaN(displayLocalPosition.y) || float.IsNaN(displayLocalPosition.z));
				set => displayLocalPosition = value ? Vector3.zero : new Vector3(float.NaN, float.NaN, float.NaN);
			}
		}


		[FieldOffset(CustomDataOffset)]
		public HealthVisualData HealthVisual;
	}

	[CreateAssetMenu(fileName = "HealthVisual", menuName = "BehaviourModifier/HealthVisual")]
	public class HealthVisualMod : LiteNetworkMod
	{
		public override System.Type DataType => typeof(LiteNetworkedData.HealthVisualData);

		public class HealthVisualContext
		{
			public LiteNetworkObject NetworkContext;
			public FillBar FillBar;
		}

		// TODO: Calculate the position of the bar based on the size of the target (SpriteRenderer.bounds.size.y)?
		// TODO: Pool the fillbar prefabs
		[SerializeField, Required] private FillBar HealthVisualBarPrefab;
		[SerializeField] private Vector3 defaultDisplayPosition = new Vector3(0, 40, 0);

		public override void SetDefaults(ref LiteNetworkedData data) =>
			SetDefaults(ref data, defaultDisplayPosition);
		public void SetDefaults(ref LiteNetworkedData data, Vector3 displayPosition)
		{
			base.SetDefaults(ref data);

			data.HealthVisual.maxHealth = 1;
			data.HealthVisual.displayLocalPosition = displayPosition;
		}

		public override void SetRuntime(ref LiteNetworkedData data)
		{
			base.SetRuntime(ref data);

			if (HealthVisualBarPrefab == null) data.HealthVisual.DisplayValid = false;
		}

		public override void Initialize(LiteNetworkObject networkContext, in LiteNetworkedData data, out object context)
		{
			var _context = new HealthVisualContext { NetworkContext = networkContext };
			context = _context;

			if (!data.HealthVisual.DisplayValid)
			{
				UnityEngine.Debug.LogWarning("HealthVisualMod: Target does not have a valid display position. LocalPosition: " + data.HealthVisual.displayLocalPosition + ". Data: " + data.ToString());
				return;
			}

			if (HealthVisualBarPrefab == null)
			{
				UnityEngine.Debug.LogWarning("HealthVisualMod: Target does not have a FillBar prefab but display was enabled. LocalPosition: " + data.HealthVisual.displayLocalPosition + ". Data: " + data.ToString());
				return;
			}

			_context.FillBar = Instantiate(HealthVisualBarPrefab.gameObject, networkContext.Target.transform).GetComponent<FillBar>();
			_context.FillBar.RawValueRange.x = 0;
		}

		public override void OnRender(object context, in LiteNetworkedData data)
		{
			if (!(context is HealthVisualContext HealthVisualContext)) return;
			if (HealthVisualContext.FillBar == null)
			{
				UnityEngine.Debug.LogWarning("HealthVisualMod: context display does not exist! This mod should always have a display so remove if not intended.");
			}
			
			if (!data.HealthVisual.DisplayValid)
			{
				UnityEngine.Debug.LogWarning("HealthVisualMod: context display exists but display is false. Destroying display.");
				Destroy(HealthVisualContext.NetworkContext.Target);
				return;
			}

			HealthVisualContext.FillBar.transform.localPosition = data.HealthVisual.displayLocalPosition;

			HealthVisualContext.FillBar.RawValueRange.y = data.HealthVisual.maxHealth;
			HealthVisualContext.FillBar.RawValue = data.HealthVisual.currentHealth;
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}
	}
}