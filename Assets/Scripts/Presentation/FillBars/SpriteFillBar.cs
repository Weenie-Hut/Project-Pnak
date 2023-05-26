using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public class SpriteFillBar : FillBar
	{

		[Tooltip("The transform that will be scaled to represent the fill amount. This transform should be filled when scale.x is 1, and empty when scale.x is 0.")]
		[SerializeField] private Transform Foreground;
		[Tooltip("The text that will be updated with the format.")]
		[SerializeField] private TMPro.TextMeshPro Text;

		public override void SyncVisuals()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && (Foreground == null || Text == null))
				return;
#endif

			Foreground.localScale = new Vector2(Mathf.Max(NormalizedValue, 1e-4f), 1);
			Text.text = FormattedValue;
		}
	}
}