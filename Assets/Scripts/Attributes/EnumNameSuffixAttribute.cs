using System;
using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public class EnumNameSuffixAttribute : PropertyAttribute
	{
		public Type EnumType { get; private set; }
		public string Fallback { get; set; }

		public EnumNameSuffixAttribute(Type enumType, string fallback = "Custom")
		{
			EnumType = enumType;
			Fallback = fallback;
		}
	}
}