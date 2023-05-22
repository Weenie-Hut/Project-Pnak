using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteFillBar : MonoBehaviour
{
	[SerializeField, Range(0, 1)]
	[Tooltip("Current fill amount of the bar. 0 = empty, 1 = full. Changing this value will automatically update the visuals but only if in the editor.")]
	private float _fillAmount = 0.24f;
	
	[Tooltip("Format string for the text. {0} will be replaced with the fill amount as a percentage.")]
	public string TextFormat = "Loading... {0:P2}";

	[Tooltip("The transform that will be scaled to represent the fill amount. This transform should be filled when scale.x is 1, and empty when scale.x is 0.")]
	[SerializeField] private Transform FillBar;
	[Tooltip("The text that will be updated with the format.")]
	[SerializeField] private TMPro.TextMeshPro Text;

	/// <summary>
	/// Current fill amount of the bar. 0 = empty, 1 = full.
	/// Setting this value will automatically update the visuals.
	/// </summary>
	public float Value
	{
		get { return _fillAmount; }
		set {
			if (value == _fillAmount) return;

			_fillAmount = Mathf.Clamp01(value);
			SyncVisuals();
		}
	}


	public void SyncVisuals()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying && (FillBar == null || Text == null))
			return;
#endif

		FillBar.localScale = new Vector2(Mathf.Max(Value, 1e-4f), 1);
		Text.text = string.Format(TextFormat, Value);
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		UnityEditor.EditorApplication.delayCall += SyncVisuals;
	}
#endif
}
