/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEngine;

namespace Pnak
{
	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class TabAttribute : PropertyAttribute
	{
		public readonly int TabCount;

		public TabAttribute(int TabCount = 1) {
			this.TabCount = TabCount;
			order = -150;
		}
	}
}

