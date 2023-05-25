using UnityEngine;

namespace Pnak
{
	public abstract class RadialOptionSO : ScriptableObject
	{
		public string Title;
		public Sprite Icon;
		public string Description;

		public abstract void OnSelect(Interactable interactable = null);
		public virtual bool IsSelectable(Interactable interactable = null) => true;
	}
}