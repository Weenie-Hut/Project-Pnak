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


		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_AddModifier(Modifier modifier)
		{
			OnModifierAdded?.Invoke(modifier);

			if (modifier.expirationType == ExpirationType.None)
				return;
		}
	}
}