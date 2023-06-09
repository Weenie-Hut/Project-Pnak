using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public class MovementAmount : Stackable<MovementAmount>, Copyable<MovementAmount>
	{
		[NaNButton, Suffix("units/sec")] public float MovementSpeed = 1f;
		[NaNButton, Min(0.0f), Suffix("sec")] public float HoldDuration = 1f;

		public MovementAmount Copy()
		{
			return new MovementAmount
			{
				MovementSpeed = MovementSpeed,
				HoldDuration = HoldDuration
			};
		}

		public void StackWith(MovementAmount other, ValueStackingType stackingType)
		{
			ValueStack.StackInPlace(this, other, stackingType);
		}
	}

	public static partial class ValueStack
	{

		public static void StackInPlace(MovementAmount a, MovementAmount b, ValueStackingType stackingType)
		{
			a.MovementSpeed = Stack(a.MovementSpeed, b.MovementSpeed, stackingType);
			a.HoldDuration = Stack(a.HoldDuration, b.HoldDuration, stackingType);
		}

		public static MovementAmount Stack(MovementAmount a, MovementAmount b, ValueStackingType stackingType)
		{
			var result = a.Copy();
			StackInPlace(result, a, stackingType);
			return result;
		}
	}
}