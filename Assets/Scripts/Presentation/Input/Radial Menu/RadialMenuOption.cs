using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pnak
{
	public class RadialMenuOption : MonoBehaviour
	{
		public RectTransform RectTransform => transform as RectTransform;

		public bool Hovered
		{
			get => _Hovered;
			set
			{
				if (_Hovered != value)
				{
					_Hovered = value;
					if (_Hovered)
					{
						OnHoverEnter?.Invoke();
					}
					else
					{
						OnHoverExit?.Invoke();
					}
				}
			}
		}

		[SerializeField] private Image _Icon;
		[SerializeField] private TMPro.TextMeshProUGUI _TitleText;
		[SerializeField] private TMPro.TextMeshProUGUI _DescriptionText;

		public UnityEvent OnDisabled;
		public UnityEvent OnEnabledAfford;
		public UnityEvent OnEnabledNoAfford;
		public UnityEvent OnHoverEnter;
		public UnityEvent OnHoverExit;

		public void SetData(RadialOptionSO data = null)
		{
			if (data == null)
			{
				_Icon.sprite = null;
				_TitleText.text = "";
				_DescriptionText.text = "";
				OnDisabled?.Invoke();
				return;
			}

			_Icon.sprite = data.Icon;
			_TitleText.text = data.Title;
			_DescriptionText.text = data.Description;
		}

		public void UpdateAffordability(bool selectable)
		{
			if (selectable)
				OnEnabledAfford?.Invoke();
			else OnEnabledNoAfford?.Invoke();
		}

		[SerializeField] private bool _Hovered = false;
		private void OnValidate()
		{
			Hovered = _Hovered;
		}
	}
}