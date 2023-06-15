/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using System.Linq;
using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class HideIfAttribute : PropertyAttribute
	{
		public MutliType[] EqualsOrArgs { get; protected set; }
		public bool Invert { get; protected set; }

		public HideIfAttribute(params object[] equalsOrArgs)
		{
			EqualsOrArgs = equalsOrArgs.Select(x => MutliType.Create(x)).ToArray();
			Invert = false;
			order = -1000;
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class ShowIfAttribute : HideIfAttribute
	{
		public ShowIfAttribute(params object[] equalsOrArgs)
		{
			EqualsOrArgs = equalsOrArgs.Select(x => MutliType.Create(x)).ToArray();
			Invert = true;
		}
	}
}

