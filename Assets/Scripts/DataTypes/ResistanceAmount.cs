using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public class ResistanceAmount : Copyable<ResistanceAmount>, Stackable<ResistanceAmount>
	{
		[NaNButton]
		public float AllMultiplier = 1f;
		[NaNButton]
		public float PhysicalMultiplier = 1f;
		[NaNButton]
		public float MagicalMultiplier = 1f;

		// public void Scale(float scale)
		// {
		// 	AllMultiplier = (AllMultiplier - 1f) * scale + 1f;
		// 	PhysicalMultiplier = (PhysicalMultiplier - 1f) * scale + 1f;
		// 	MagicalMultiplier = (MagicalMultiplier - 1f) * scale + 1f;
		// }

		public ResistanceAmount Copy()
		{
			return new ResistanceAmount
			{
				AllMultiplier = AllMultiplier,
				PhysicalMultiplier = PhysicalMultiplier,
				MagicalMultiplier = MagicalMultiplier
			};
		}

		override public string ToString()
		{
			return $"{{All: {AllMultiplier}, Physical: {PhysicalMultiplier}, Magical: {MagicalMultiplier}}}";
		}

		public void StackWith(ResistanceAmount other, ValueStackingType stackingType)
		{
			// UnityEngine.Debug.Log($"Stacking {this} with {other} using {stackingType}");
			ValueStack.StackInPlace(this, other, stackingType);
			// UnityEngine.Debug.Log($"Result: {this}");
		}
	}

	public static partial class ValueStack
	{
		// public static void StackInPlace(ref DamageAmount a, DamageAmount b, ValueStackingType stackingType)
		// {
		// 	a.PhysicalDamage = Stack(a.PhysicalDamage, b.PhysicalDamage, stackingType);
		// 	a.MagicalDamage = Stack(a.MagicalDamage, b.MagicalDamage, stackingType);
		// 	a.PureDamage = Stack(a.PureDamage, b.PureDamage, stackingType);
		// 	a.ApplyModifiers = Stack(a.ApplyModifiers, b.ApplyModifiers, stackingType);
		// }

		// public static DamageAmount Stack(DamageAmount a, DamageAmount b, ValueStackingType stackingType)
		// {
		// 	var result = a.Copy();
		// 	StackInPlace(ref result, a, stackingType);
		// 	return result;
		// }

		public static void StackInPlace(ResistanceAmount a, ResistanceAmount b, ValueStackingType stackingType)
		{
			a.AllMultiplier = Stack(a.AllMultiplier, b.AllMultiplier, stackingType);
			a.PhysicalMultiplier = Stack(a.PhysicalMultiplier, b.PhysicalMultiplier, stackingType);
			a.MagicalMultiplier = Stack(a.MagicalMultiplier, b.MagicalMultiplier, stackingType);
		}

		public static ResistanceAmount Stack(ResistanceAmount a, ResistanceAmount b, ValueStackingType stackingType)
		{
			var result = a.Copy();
			StackInPlace(result, a, stackingType);
			return result;
		}
	}
}