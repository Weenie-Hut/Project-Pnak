using System.Linq;
using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class ButtonAttribute : PropertyAttribute
	{
		public string MethodName { get; protected set; }
		public string ButtonName { get; protected set; }
		public string Tooltip { get; protected set; }
		public MutliType[] HideWhen { get; protected set; }


		public ButtonAttribute(string methodName, string buttonName = null, string tooltip = null, params object[] hideWhen)
		{
			MethodName = methodName;
			ButtonName = buttonName;
			Tooltip = tooltip;
			HideWhen = hideWhen.Select(x => MutliType.Create(x)).ToArray();
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class NaNButtonAttribute : ButtonAttribute
	{
		public NaNButtonAttribute(params object[] hideWhen)
			: base("SetNAN", "NaN", "Prevents value from being used when stacking/modifying other data", hideWhen)
		{
		}
	}
}

