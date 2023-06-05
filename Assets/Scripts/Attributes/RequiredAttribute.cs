using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class RequiredAttribute : PropertyAttribute
	{
		public RequiredAttribute() {}
	}
}

