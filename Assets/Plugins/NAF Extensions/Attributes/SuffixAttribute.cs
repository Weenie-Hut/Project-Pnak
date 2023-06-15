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
	public class SuffixAttribute : PropertyAttribute
	{
		public string Suffix { get; private set; }
		public string Tooltip { get; set; }

		public SuffixAttribute(string suffix, string tooltip = null)
		{
			Suffix = suffix;
			Tooltip = tooltip;
			order = -1;
		}
	}
}

