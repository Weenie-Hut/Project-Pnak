using System;
using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class AsEnumAttribute : PropertyAttribute
	{
		public Type EnumType { get; private set; }

		public AsEnumAttribute(Type type)
		{
			EnumType = type;
		}
	}
}

