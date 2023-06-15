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
	public class DefaultAttribute : PropertyAttribute
	{
		public string sValue { get; private set; }
		public int iValue { get; private set; }
		public float fValue { get; private set; }
		public bool bValue { get; private set; }
		public long lValue { get; private set; }
		public Vector2 v2Value { get; private set; }
		public Vector3 v3Value { get; private set; }

		public DefaultAttribute(string value) => sValue = value;
		public DefaultAttribute(int value) => iValue = value;
		public DefaultAttribute(float value) => fValue = value;
		public DefaultAttribute(bool value) => bValue = value;
		public DefaultAttribute(long value) => lValue = value;
		public DefaultAttribute(Vector2 value) => v2Value = value;
		public DefaultAttribute(Vector3 value) => v3Value = value;
	}
}

