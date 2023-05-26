using UnityEngine;

namespace Pnak
{
	public abstract class FillBar : MonoBehaviour
	{
		public enum FormatType
		{
			/// <summary>
			/// The value will be displayed as a fractional value between 0 and 1.
			/// </summary>
			Normalized,
			/// <summary>
			/// The value will be displayed as is (fill bar will still use a normalized value).
			/// </summary>
			RawValue,
		}

		[SerializeField, Range(0, 1)]
		[Tooltip("Current fill amount of the bar. 0 = empty, 1 = full. Changing this value will automatically update the visuals but only if in the editor.")]
		private float _normalizedValue = 0.24f;

		[Tooltip("The range of the raw value. This is used for converting the raw value to a normalized value and vice versa.")]
		public Vector2 RawValueRange = new Vector2(0.0f, 1.0f);

		[Tooltip("The format type of the value. Normalized will display the value as a percentage, RawValue will display the value as is.")]
		public FormatType Format = FormatType.Normalized;
		
		[Tooltip("Format string for the text. {0} will be replaced with the fill amount according to string.Format().")]
		public string TextFormat = "Loading... {0:P2}";

		/// <summary>
		/// Current fill amount of the bar as a percentage: 0 = empty, 1 = full.
		/// Setting this value will automatically update the visuals.
		/// </summary>
		public float NormalizedValue
		{
			get { return _normalizedValue; }
			set {
				if (value == _normalizedValue) return;

				_normalizedValue = Mathf.Clamp01(value);
				SyncVisuals();
			}
		}

		/// <summary>
		/// Current fill amount of the bar as a raw value.
		/// Setting this value will automatically update the visuals.
		/// </summary>
		public float RawValue
		{
			get { return Mathf.Lerp(RawValueRange.x, RawValueRange.y, NormalizedValue); }
			set {
				NormalizedValue = Mathf.InverseLerp(RawValueRange.x, RawValueRange.y, value);
			}
		}

		public string FormattedValue
		{
			get { return string.Format(TextFormat, Format == FormatType.Normalized ? NormalizedValue : RawValue); }
		}

		public abstract void SyncVisuals();

	#if UNITY_EDITOR
		private void OnValidate()
		{
			UnityEditor.EditorApplication.delayCall += SyncVisuals;
		}
	#endif
	}
}