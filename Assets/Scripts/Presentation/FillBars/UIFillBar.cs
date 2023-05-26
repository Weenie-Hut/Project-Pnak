using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public class UIFillBar : FillBar
	{
		[Tooltip("The transform that will be scaled to represent the fill amount. This transform should be filled when anchorMax.x is 1, and empty when anchorMax.x is 0.")]
		[SerializeField] private RectTransform Foreground;

		[Tooltip("The text that will be updated with the format.")]
		[SerializeField] private TMPro.TextMeshProUGUI Text;

		public override void SyncVisuals()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && (Foreground == null || Text == null))
				return;
#endif

			Foreground.anchorMax = new Vector2(NormalizedValue, 1);
			Text.text = FormattedValue;
		}
	}
}
