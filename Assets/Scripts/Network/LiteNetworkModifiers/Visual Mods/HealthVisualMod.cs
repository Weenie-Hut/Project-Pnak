using System;
using System.Collections;
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
			public float lastHealth;
			public int? lastChangeTick;
		}

		// TODO: Calculate the position of the bar based on the size of the target (SpriteRenderer.bounds.size.y)?
		// TODO: Pool the fillbar prefabs
		[SerializeField, Required, Searchable]
		private FillBar HealthVisualBarPrefab;

		[SerializeField, Searchable]
		private CartoonFX.CFXR_ParticleText DamageTextPrefab;
		private ComponentPool<CartoonFX.CFXR_ParticleText> DamageTextPool;

		[SerializeField]
		[Tooltip("The time between each time damage is shown."), Suffix("sec"), MinMax(min: 0.0333f)]
		private float IntervalBetweenDamage = 1f;

		[AsLabel(LabelType.Italic | LabelType.Right, "Damage is shown every {0} network tick(s)")]
		[SerializeField] private int intervalInTicks;

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

			if (DamageTextPrefab != null)
			{
				bool change = false;

				if (HealthVisualContext.lastChangeTick.HasValue)
				{
					if (HealthVisualContext.lastHealth != data.HealthVisual.currentHealth)
					{
						int ticksSinceLastChange = SessionManager.Tick - HealthVisualContext.lastChangeTick.Value;
						if (ticksSinceLastChange >= intervalInTicks)
						{
							change = true;
							SpawnDamageText(
								HealthVisualContext.NetworkContext.Target.transform.position,
								HealthVisualContext.lastHealth - data.HealthVisual.currentHealth,
								data.HealthVisual.maxHealth);
						}
					}
				}
				else change = true;

				if (change)
				{
					// UnityEngine.Debug.Log("HealthVisualMod: " + HealthVisualContext.NetworkContext.Target.name + " health changed from " + HealthVisualContext.lastHealth + " to " + data.HealthVisual.currentHealth + ".");
					HealthVisualContext.lastChangeTick = SessionManager.Tick;
					HealthVisualContext.lastHealth = data.HealthVisual.currentHealth;
				}
			}
		}

		private void SpawnDamageText(Vector3 position, float amount, float max)
		{
			if (DamageTextPool == null)
				DamageTextPool = new ComponentPool<CartoonFX.CFXR_ParticleText>(DamageTextPrefab);

			var damageText = DamageTextPool.Get();
			damageText.transform.position = position;

			var baseSize = DamageTextPrefab.transform.localScale;
			float normDamage = amount / max;
			damageText.transform.localScale = baseSize * Mathf.Lerp(0.5f, 1.5f, normDamage);

			damageText.UpdateText(amount.ToString("0.##"));
			
			damageText.StartCoroutine(ReturnToPool(damageText));
		}

		private IEnumerator ReturnToPool(CartoonFX.CFXR_ParticleText damageText)
		{
			var particles = damageText.GetComponent<ParticleSystem>();
			particles.Play(true);
			while (particles.isPlaying) yield return null;

			DamageTextPool.Return(damageText);
		}

		public override void OnInvalidatedRender(object context, in LiteNetworkedData data)
		{
			if (!(context is FillBar fillBar)) return;
			Destroy(fillBar.gameObject);
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			intervalInTicks = Mathf.RoundToInt(IntervalBetweenDamage / SessionManager.DeltaTime);
		}
	}
}