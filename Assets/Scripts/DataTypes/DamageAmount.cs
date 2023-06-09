using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	public interface Copyable<T>
	{
		public T Copy();
	}

	public interface Stackable<T>
	{
		public void StackWith(T other, ValueStackingType stackingType);
	}

	[System.Serializable]
	public class DamageAmount : Copyable<DamageAmount>, Stackable<DamageAmount>
	{
		[NaNButton]
		public float PhysicalDamage = float.NaN;
		[NaNButton]
		public float MagicalDamage = float.NaN;
		[NaNButton]
		public float PureDamage = float.NaN;
		[Tooltip("Modifiers to apply to the target when this damage is applied.")]
		public SerializedLiteNetworkedData[] ApplyModifiers;

		public override string ToString()
		{
			if (PhysicalDamage == MagicalDamage && PhysicalDamage == PureDamage)
				return PhysicalDamage.ToString();
			else {
				string result = "";
				if (PhysicalDamage.NaNTo0() != 0)
					result += PhysicalDamage + " Physical";
				if (MagicalDamage.NaNTo0() != 0)
					result += (result.Length > 0 ? ", " : "") + MagicalDamage + " Magical";
				if (PureDamage.NaNTo0() != 0)
					result += (result.Length > 0 ? ", " : "") + PureDamage + " Pure";
				return result;
			}
		}

		public string Format(string format)
		{
			return format.FormatById("physical", PhysicalDamage)
				.FormatById("magical", MagicalDamage)
				.FormatById("pure", PureDamage);
		}

		public DamageAmount Copy()
		{
			return new DamageAmount
			{
				PhysicalDamage = PhysicalDamage,
				MagicalDamage = MagicalDamage,
				PureDamage = PureDamage,
				ApplyModifiers = ApplyModifiers
			};
		}

		public void StackWith(DamageAmount other, ValueStackingType stackingType)
		{
			ValueStack.StackInPlace(this, other, stackingType);
		}

		public void Scale(float scale)
		{
			if (float.IsNaN(scale))
				return;

			if(!float.IsNaN(PhysicalDamage))
				PhysicalDamage *= scale;

			if(!float.IsNaN(MagicalDamage))
				MagicalDamage *= scale;

			if(!float.IsNaN(PureDamage))
				PureDamage *= scale;
		}
	}

	public static partial class ValueStack
	{
		// public static ShootData Stack(ShootData a, ShootData b, ValueStackingType stackingType)
		// {
		// 	return new ShootData
		// 	{
		// 		ReloadTime = Stack(a.ReloadTime, b.ReloadTime, stackingType),
		// 		FireCountRange = Stack(a.FireCountRange, b.FireCountRange, stackingType),
		// 		FireSpreadAngle = Stack(a.FireSpreadAngle, b.FireSpreadAngle, stackingType),
		// 		Spawn = Stack(a.Spawn, b.Spawn, stackingType),
		// 		MunitionMods = Stack(a.MunitionMods, b.MunitionMods, stackingType),
		// 		DamageMods = Stack(a.DamageMods, b.DamageMods, stackingType)
		// 	};
		// }

		public static void StackInPlace(DamageAmount a, DamageAmount b, ValueStackingType stackingType)
		{
			a.PhysicalDamage = Stack(a.PhysicalDamage, b.PhysicalDamage, stackingType);
			a.MagicalDamage = Stack(a.MagicalDamage, b.MagicalDamage, stackingType);
			a.PureDamage = Stack(a.PureDamage, b.PureDamage, stackingType);
			a.ApplyModifiers = Stack(a.ApplyModifiers, b.ApplyModifiers, stackingType);
		}

		public static DamageAmount Stack(DamageAmount a, DamageAmount b, ValueStackingType stackingType)
		{
			var result = a.Copy();
			StackInPlace(result, a, stackingType);
			return result;
		}
	}
}