using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkObject
	{
		public bool IsReserved;
		public List<int> Modifiers;
		public GameObject Target;
		public int PrefabIndex;

		public LiteNetworkObject()
		{
			IsReserved = false;
			Modifiers = new List<int>();
			Target = null;
			PrefabIndex = -1;
		}
	}
}