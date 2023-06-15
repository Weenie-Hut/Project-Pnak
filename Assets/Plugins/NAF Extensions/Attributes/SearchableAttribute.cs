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
	public class SearchableAttribute : PropertyAttribute
	{
		public bool AssetsOnly { get; private set; }
		public Type[] RequiredComponents { get; private set; }
		public bool IncludeChildren { get; private set; }

		public SearchableAttribute(params Type[] requiredComponents) : this(false, requiredComponents)
		{
		}

		public SearchableAttribute(bool assetsOnly, params Type[] requiredComponents) : this(assetsOnly, false, requiredComponents)
		{
		}

		public SearchableAttribute(bool assetsOnly, bool includeChildren, params Type[] requiredComponents)
		{
			AssetsOnly = assetsOnly;
			RequiredComponents = requiredComponents;
			IncludeChildren = includeChildren;
			order = -50;
		}
	}
}

