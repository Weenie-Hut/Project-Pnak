using System;
using UnityEngine;

namespace Pnak
{
	[Flags]
	public enum LabelType
	{
		None = 0,
		Italic = 1 << 0,
		Bold = 1 << 1,
		Mini = 1 << 2,
	}

	[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class AsLabelAttribute : PropertyAttribute
	{
		public LabelType Type { get; private set; }

		public AsLabelAttribute(LabelType type = LabelType.None)
		{
			Type = type;
		}
	}
}

