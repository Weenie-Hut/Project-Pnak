using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class HideIfAttribute : PropertyAttribute
	{
		public string Expression { get; private set; }
		public bool Invert { get; private set; }

		public HideIfAttribute(string Expression, bool invert = false)
		{
			this.Expression = Expression;
			this.Invert = invert;
		}
	}
}

