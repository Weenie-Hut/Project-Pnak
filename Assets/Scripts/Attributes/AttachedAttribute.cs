using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class AttachedAttribute : PropertyAttribute
	{
		public bool IncludeChildren { get; private set; }
		public bool Required { get; private set; }

		public AttachedAttribute(bool includeChildren = false, bool required = true)
		{
			IncludeChildren = includeChildren;
			Required = required;
		}
	}
}