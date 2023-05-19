using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "CharacterData", menuName = "Pnak/CharacterData")]
	public class CharacterData : ScriptableObject
	{
		public string Name;
		public Sprite Sprite;
		public float Speed;
		public Vector2 UIScale;
		public Vector2 UIPosition;
		public Vector2 SpriteScale;
		public Vector2 SpritePosition;
	}
}