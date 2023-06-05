using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public struct DamageAmount
	{
		public float PhysicalDamage;
		public float MagicalDamage;
		public float PureDamage;
		[Tooltip("Modifiers to apply to the target when this damage is applied.")]
		[Required, SerializeField]
		private List<StateModifierSO> applyModifiers;
		private List<StateModifier> runtimeModifiers;

		public List<StateModifierSO> ApplyModifiers => applyModifiers ?? (applyModifiers = new List<StateModifierSO>());
		public List<StateModifier> RuntimeModifiers => runtimeModifiers ?? (runtimeModifiers = new List<StateModifier>());
		
		public static implicit operator DamageAmount(float value)
		{
			return new DamageAmount
			{
				PhysicalDamage = value,
				MagicalDamage = value,
				PureDamage = value
			};
		}

		public static DamageAmount operator +(DamageAmount damage, DamageAmount other)
		{
			return new DamageAmount
			{
				PhysicalDamage = damage.PhysicalDamage + other.PhysicalDamage,
				MagicalDamage = damage.MagicalDamage + other.MagicalDamage,
				PureDamage = damage.PureDamage + other.PureDamage,
				applyModifiers = damage.ApplyModifiers.Concat(other.ApplyModifiers).ToList(),
				runtimeModifiers = damage.RuntimeModifiers.Concat(other.RuntimeModifiers).ToList()
			};
		}

		public static DamageAmount operator *(DamageAmount damage, DamageAmount other)
		{
			return new DamageAmount
			{
				PhysicalDamage = damage.PhysicalDamage * other.PhysicalDamage,
				MagicalDamage = damage.MagicalDamage * other.MagicalDamage,
				PureDamage = damage.PureDamage * other.PureDamage,
				applyModifiers = damage.ApplyModifiers.Concat(other.ApplyModifiers).ToList(),
				runtimeModifiers = damage.RuntimeModifiers.Concat(other.RuntimeModifiers).ToList()
			};
		}

		public static DamageAmount operator -(DamageAmount damage, DamageAmount other)
		{
			List<StateModifierSO> modifiers = damage.ApplyModifiers.ToList();
			foreach (StateModifierSO modifier in other.ApplyModifiers)
				modifiers.Remove(modifier);

			List<StateModifier> runtimeModifiers = damage.RuntimeModifiers.ToList();
			foreach (StateModifier modifier in other.RuntimeModifiers)
				runtimeModifiers.Remove(modifier);

			return new DamageAmount
			{
				PhysicalDamage = damage.PhysicalDamage - other.PhysicalDamage,
				MagicalDamage = damage.MagicalDamage - other.MagicalDamage,
				PureDamage = damage.PureDamage - other.PureDamage,
				applyModifiers = modifiers,
				runtimeModifiers = runtimeModifiers
			};
		}

		public static DamageAmount operator /(DamageAmount damage, DamageAmount other)
		{
			List<StateModifierSO> modifiers = damage.ApplyModifiers.ToList();
			foreach (StateModifierSO modifier in other.ApplyModifiers)
				modifiers.Remove(modifier);

			List<StateModifier> runtimeModifiers = damage.RuntimeModifiers.ToList();
			foreach (StateModifier modifier in other.RuntimeModifiers)
				runtimeModifiers.Remove(modifier);
			
			return new DamageAmount
			{
				PhysicalDamage = damage.PhysicalDamage / other.PhysicalDamage,
				MagicalDamage = damage.MagicalDamage / other.MagicalDamage,
				PureDamage = damage.PureDamage / other.PureDamage,
				applyModifiers = modifiers,
				runtimeModifiers = runtimeModifiers
			};
		}

		public static DamageAmount operator -(DamageAmount damage)
		{
			return new DamageAmount
			{
				PhysicalDamage = -damage.PhysicalDamage,
				MagicalDamage = -damage.MagicalDamage,
				PureDamage = -damage.PureDamage
			};
		}

		public override string ToString()
		{
			if (PhysicalDamage == MagicalDamage && PhysicalDamage == PureDamage)
				return PhysicalDamage.ToString();
			else {
				string result = "";
				if (PhysicalDamage != 0)
					result += PhysicalDamage + " Physical";
				if (MagicalDamage != 0)
					result += (result.Length > 0 ? ", " : "") + MagicalDamage + " Magical";
				if (PureDamage != 0)
					result += (result.Length > 0 ? ", " : "") + PureDamage + " Pure";
				return result;
			}
		}

		public string ToString(string format)
		{
			if (PhysicalDamage == MagicalDamage && PhysicalDamage == PureDamage)
				return PhysicalDamage.ToString(format);
			else {
				string result = "";
				if (PhysicalDamage != 0)
					result += PhysicalDamage.ToString(format) + " Physical";
				if (MagicalDamage != 0)
					result += (result.Length > 0 ? ", " : "") + MagicalDamage.ToString(format) + " Magical";
				if (PureDamage != 0)
					result += (result.Length > 0 ? ", " : "") + PureDamage.ToString(format) + " Pure";
				return result;
			}
		}

		internal static DamageAmount Min(DamageAmount current, DamageAmount newValue)
		{
			throw new NotImplementedException();
		}

		internal static DamageAmount Max(DamageAmount current, DamageAmount newValue)
		{
			throw new NotImplementedException();
		}
	}

	
}