using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	[CreateAssetMenu(fileName = "SpawnPattern", menuName = "Pnak/SpawnPattern")]
	public class SpawnPattern : ScriptableObject
	{
		[System.Serializable]
		public struct SpawnData
		{
			[Tooltip("The delay in seconds from the previous spawn time.")]
			public float delay;

			public float speed;

			public float health;
		}

		public List<SpawnData> Data;
		public bool Loop;

		public SpawnData this[int index]
		{
			get
			{
				return Data[index];
			}
		}

		public int Length => Data.Count;
	}
}