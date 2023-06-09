using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class HideLabelAttribute : PropertyAttribute
	{
		public HideLabelAttribute() {}
	}
}

