/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

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

