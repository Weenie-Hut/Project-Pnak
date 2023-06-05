using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkObject
	{
		public bool IsReserved;
		public List<int> Modifiers;
		public StateBehaviourController Target;
		public int PrefabIndex;
		public int Index;

		public LiteNetworkObject(int index)
		{
			IsReserved = false;
			Modifiers = new List<int>();
			Target = null;
			PrefabIndex = -1;
			Index = index;
		}

		public string Format()
		{
			return $"{{\n\tIsReserved: {IsReserved}" +
				"\n\tModifiers:\n\t\t" + Modifiers.Select(addr => $"Addr {addr} = {LiteNetworkManager.GetModContext(addr)}").Format("\n\t\t") +
				$"\n\tTarget: {Target}" +
				$"\n\tPrefabIndex: {PrefabIndex}\n}}";
		}
	}
}