using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class ButtonAttribute : PropertyAttribute
	{
		public string MethodName { get; private set; }
		public string ButtonName { get; private set; }
		public string HideWhen { get; private set; }

		public ButtonAttribute(string methodName, string buttonName = null, string hideWhen = null)
		{
			MethodName = methodName;
			ButtonName = buttonName;
			HideWhen = hideWhen;
		}
	}
}

