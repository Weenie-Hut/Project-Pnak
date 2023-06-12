using UnityEngine;

namespace Pnak
{
	public interface Formatable
	{
		string Format(string format);
	}

	public abstract class RadialOptionSO : ScriptableObject, Formatable
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




		public string Title => Format(TitleFormat);
		public string Description => Format(DescriptionFormat);

		public virtual string Format(string format) => Format(format, null);
		public virtual string Format(string format, Interactable interactable) => format;

		public abstract void OnSelect(Interactable interactable = null);
		public virtual bool IsSelectable(Interactable interactable = null) => true;
		public virtual bool IsValidTarget(Interactable interactable = null) => true;
		
#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			TitlePreview = Title;
			DescriptionPreview = Description;
		}
#endif
	}
}