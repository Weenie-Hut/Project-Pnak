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
		public float Speed = 1f;
		public float ReloadTime = 1f;
		public float MP_Max = 60f;
		public float MP_RegenerationRate = 1f;
		public Munition ProjectilePrefab;
		public float TowerPlacementTime = 10f;
		public Tower TowerPrefab;
		public Vector2 UIScale;
		public Vector2 UIPosition;
		public Vector2 SpriteScale;
		public Vector2 SpritePosition;
	}
}