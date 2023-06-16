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
	public class FixedIntervalAttribute : PropertyAttribute
	{
		public readonly string DescriptionFormat;
		public readonly float DeltaTime;
		public readonly string CanBeZero;

		public FixedIntervalAttribute() : this("{0:F2} seconds", null, 1 / 60f) {}

		public FixedIntervalAttribute(string descriptionFormat) : this(descriptionFormat, null, 1 / 60f) {}

		public FixedIntervalAttribute(string descriptionFormat, string canBeZero) : this(descriptionFormat, canBeZero, 1 / 60f) {}

		public FixedIntervalAttribute(string descriptionFormat, string canBeZero, float deltaTime)
		{
			DescriptionFormat = descriptionFormat;
			CanBeZero = canBeZero;
			DeltaTime = deltaTime;
		}
	}
}

