using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public class CharacterSelectUI : MonoBehaviour
	{
		[SerializeField] private TMPro.TextMeshProUGUI _characterName;
		[SerializeField] private UnityEngine.UI.Image _characterImage;

		public void SetData(CharacterData characterData)
		{
			_characterName.text = characterData.Name;
			_characterImage.sprite = characterData.Sprite;
			_characterImage.rectTransform.sizeDelta = characterData.UIScale;
			_characterImage.rectTransform.anchoredPosition = characterData.UIPosition;
		}
	}
}
