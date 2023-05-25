using System.Collections;
using System.Collections.Generic;
using Fusion;

namespace Pnak
{
	public class ModifierContainer : NetworkBehaviour
	{
		public delegate void ModifierDelegate(Modifier modifier);

		public event ModifierDelegate OnModifierAdded;
		public event ModifierDelegate OnModifierRemoved;

		public List<Modifier> Modifiers { get; private set; } = new List<Modifier>();

		public IEnumerable<Modifier> GetModifiers()
		{
			return Modifiers;
		}

		public IEnumerable<Modifier> GetModifiersOfType(ModifierTarget type)
		{
			foreach (Modifier modifier in Modifiers)
			{
				if (modifier.type == type)
					yield return modifier;
			}
		}


		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_AddModifier(Modifier modifier)
		{
			OnModifierAdded?.Invoke(modifier);

			Modifiers.Add(modifier);

			if (modifier.expirationType == ExpirationType.None)
				return;
		}

		public void RemoveModifier(Modifier modifier)
		{
			OnModifierRemoved?.Invoke(modifier);
		}
	}
}