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
	public class ValidateAttribute : PropertyAttribute
	{
		public MutliType[] EqualsOrArgs { get; protected set; }
		public string Message { get; protected set; }

		public ValidateAttribute(params object[] equalsOrArgs)
		{
			EqualsOrArgs = equalsOrArgs.Select(x => MutliType.Create(x)).ToArray();
			Message = "Invalid";
		}
	}

	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class RequiredAttribute : ValidateAttribute
	{
		public RequiredAttribute()
		{
			EqualsOrArgs = new MutliType[0];
			Message = "Required";
		}
	}
}