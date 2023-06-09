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

		public SearchableAttribute(params Type[] requiredComponents)
		{
			AssetsOnly = false;
			RequiredComponents = requiredComponents;
			IncludeChildren = false;
		}

		public SearchableAttribute(bool assetsOnly, params Type[] requiredComponents)
		{
			AssetsOnly = assetsOnly;
			RequiredComponents = requiredComponents;
			IncludeChildren = false;
		}

		public SearchableAttribute(bool assetsOnly, bool includeChildren, params Type[] requiredComponents)
		{
			AssetsOnly = assetsOnly;
			RequiredComponents = requiredComponents;
			IncludeChildren = includeChildren;
		}
	}
}

