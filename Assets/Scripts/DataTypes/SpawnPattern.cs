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
			[Min(0.0f)]
			public float delay;
			[Required]
			public Enemy enemy;
		}

		public List<SpawnData> Data;
		public bool Loop;
		[ShowIf(nameof(Loop))]
		public float HealthScalePerLoop = 1.0f;

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