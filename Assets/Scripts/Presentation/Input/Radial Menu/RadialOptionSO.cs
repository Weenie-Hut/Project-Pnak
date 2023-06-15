using UnityEngine;

namespace Pnak
{
	public interface Formatable
	{
		string Format(string format);
	}

	public abstract class RadialOptionSO : ScriptableObject
	{
		[TextArea] public string TitleFormat;
#if UNITY_EDITOR
		[AsLabel(LabelType.Mini | LabelType.Italic)] public string TitlePreview;
#endif

		[TextArea] public string DescriptionFormat;
#if UNITY_EDITOR
		[AsLabel(LabelType.Mini | LabelType.Italic)] public string DescriptionPreview;
#endif

		[Required, HideLabel] public Sprite Icon;

		public string GetTitle(Interactable interactable = null) => Format(TitleFormat, interactable);
		public string GetDescription(Interactable interactable = null) => Format(DescriptionFormat, interactable);

		public virtual string Format(string format, Interactable interactable) => format;

		public abstract void OnSelect(Interactable interactable = null);
		public virtual bool IsSelectable(Interactable interactable = null) => true;
		public virtual bool IsValidTarget(Interactable interactable = null) => true;
		
#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			TitlePreview = GetTitle();
			DescriptionPreview = GetDescription();
		}
#endif
	}
}