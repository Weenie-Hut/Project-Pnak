using UnityEngine;

namespace Pnak
{
	public enum ValueStackingType
	{
		[Tooltip("This value cannot be stacked. Only works if all values will not stack (new modifiers will be created instead), otherwise treated like keep current. Ex: Adding DoT that independently expires, but multiple DoTs can be applied.")]
		DoNotStack,

		[Tooltip("When stacking, this field will be ignored.")]
		KeepCurrent,
		[Tooltip("When stacking, this field will be reset to the original value.")]
		Original,
		[Tooltip("When stacking, this field will be replaced with the new value.")]
		Replace,
		[Tooltip("When stacking, this field will be added to the current value.")]

		AddCurrent,
		[Tooltip("When stacking, this field will be multiplied by the current value.")]
		MultiplyCurrent,
		[Tooltip("When stacking, this field will be set to the minimum of the current and new values.")]
		MinCurrent,
		[Tooltip("When stacking, this field will be set to the maximum of the current and new values.")]
		MaxCurrent,

		[Tooltip("When stacking, this field will be added to the original value.")]
		AddOriginal,
		[Tooltip("When stacking, this field will be multiplied by the original value.")]
		MultiplyOriginal,
		[Tooltip("When stacking, this field will be set to the minimum of the original and new values.")]
		MinOriginal,
		[Tooltip("When stacking, this field will be set to the maximum of the original and new values.")]
		MaxOriginal
	}

	[System.Serializable]
	public struct StackSettings
	{
		public ValueStackingType StackingType;
		public int StackId;
	}

	[System.Serializable]
	public struct ValueStackSettings<T> where T : struct
	{
		public ValueStackingType StackingType;
		public int StackId;
		public T Value;

		public static implicit operator T(ValueStackSettings<T> settings) => settings.Value;
	}

	[System.Serializable]
	public abstract class Stackable<T, Self> where Self : Stackable<T, Self>, new() where T : struct
	{
		public static Self Create(ValueStackSettings<T> settings) => new Self
		{
			StackingType = settings.StackingType,
			StackId = settings.StackId,
			Value = settings.Value
		};

		public ValueStackingType StackingType;
		[Tooltip("The ID of the stack this modifier belongs to. Modifiers will only stack with other modifiers of the same stack ID.")]
		public int StackId;
		public T Value;

		public bool StackWith(Self other) => StackWith(other.Value, other);
		public bool StackWith(T current, Self other)
			=> StackWith(current, other.Value, other.StackId, other.StackingType);
		public bool StackWith(in ValueStackSettings<T> settings)
			=> StackWith(settings.Value, settings);
		public bool StackWith(T current, in ValueStackSettings<T> settings)
			=> StackWith(current, settings.Value, settings.StackId, settings.StackingType);

		public bool StackWith(T current, T newValue, int otherId, ValueStackingType otherStackingType)
		{
			if (StackingType != otherStackingType)
				return false;
			if (StackingType == ValueStackingType.DoNotStack)
				return false;
			if (StackId != otherId)
				return false;

			Value = Stack(current, newValue);
			return true;
		}


		public abstract T Stack(T current, T newValue);

		public static implicit operator T(Stackable<T, Self> stackable) => stackable?.Value ?? default;
	}

	public class StackableFloat : Stackable<float, StackableFloat>
	{
		public override float Stack(float current, float newValue)
		{
			float original = Value;
			ValueStackingType stackingType = StackingType;

			switch (stackingType)
			{
				case ValueStackingType.KeepCurrent:
					return current;
				case ValueStackingType.Original:
					return original;
				case ValueStackingType.Replace:
					return newValue;
				case ValueStackingType.AddCurrent:
					return current + newValue;
				case ValueStackingType.MultiplyCurrent:
					return current * newValue;
				case ValueStackingType.MinCurrent:
					return Mathf.Min(current, newValue);
				case ValueStackingType.MaxCurrent:
					return Mathf.Max(current, newValue);
				case ValueStackingType.AddOriginal:
					return original + newValue;
				case ValueStackingType.MultiplyOriginal:
					return original * newValue;
				case ValueStackingType.MinOriginal:
					return Mathf.Min(original, newValue);
				case ValueStackingType.MaxOriginal:
					return Mathf.Max(original, newValue);
				default:
					return newValue;
			}
		}
	}

	public class StackableInt : Stackable<int, StackableInt>
	{
		public override int Stack(int current, int newValue)
		{
			int original = Value;
			ValueStackingType stackingType = StackingType;

			switch (stackingType)
			{
				case ValueStackingType.KeepCurrent:
					return current;
				case ValueStackingType.Original:
					return original;
				case ValueStackingType.Replace:
					return newValue;
				case ValueStackingType.AddCurrent:
					return current + newValue;
				case ValueStackingType.MultiplyCurrent:
					return current * newValue;
				case ValueStackingType.MinCurrent:
					return Mathf.Min(current, newValue);
				case ValueStackingType.MaxCurrent:
					return Mathf.Max(current, newValue);
				case ValueStackingType.AddOriginal:
					return original + newValue;
				case ValueStackingType.MultiplyOriginal:
					return original * newValue;
				case ValueStackingType.MinOriginal:
					return Mathf.Min(original, newValue);
				case ValueStackingType.MaxOriginal:
					return Mathf.Max(original, newValue);
				default:
					return newValue;
			}
		}
	}

	public class StackableDamage : Stackable<DamageAmount, StackableDamage>
	{
		public override DamageAmount Stack(DamageAmount current, DamageAmount newValue)
		{
			DamageAmount original = Value;
			ValueStackingType stackingType = StackingType;

			switch (stackingType)
			{
				case ValueStackingType.KeepCurrent:
					return current;
				case ValueStackingType.Original:
					return original;
				case ValueStackingType.Replace:
					return newValue;
				case ValueStackingType.AddCurrent:
					return current + newValue;
				case ValueStackingType.MultiplyCurrent:
					return current * newValue;
				case ValueStackingType.MinCurrent:
					return DamageAmount.Min(current, newValue);
				case ValueStackingType.MaxCurrent:
					return DamageAmount.Max(current, newValue);
				case ValueStackingType.AddOriginal:
					return original + newValue;
				case ValueStackingType.MultiplyOriginal:
					return original * newValue;
				case ValueStackingType.MinOriginal:
					return DamageAmount.Min(original, newValue);
				case ValueStackingType.MaxOriginal:
					return DamageAmount.Max(original, newValue);
				default:
					return newValue;
			}
		}
	}

	public class StackableResistance : Stackable<ResistanceAmount, StackableResistance>
	{
		public override ResistanceAmount Stack(ResistanceAmount current, ResistanceAmount newValue)
		{
			ResistanceAmount original = Value;
			ValueStackingType stackingType = StackingType;

			switch (stackingType)
			{
				case ValueStackingType.KeepCurrent:
					return current;
				case ValueStackingType.Original:
					return original;
				case ValueStackingType.Replace:
					return newValue;
				case ValueStackingType.AddCurrent:
					return new ResistanceAmount {
						PhysicalMultiplier = current.PhysicalMultiplier + newValue.PhysicalMultiplier,
						MagicalMultiplier = current.MagicalMultiplier + newValue.MagicalMultiplier,
						AnyMultiplier = current.AnyMultiplier + newValue.AnyMultiplier,
					};
				case ValueStackingType.MultiplyCurrent:
					return new ResistanceAmount {
						PhysicalMultiplier = current.PhysicalMultiplier * newValue.PhysicalMultiplier,
						MagicalMultiplier = current.MagicalMultiplier * newValue.MagicalMultiplier,
						AnyMultiplier = current.AnyMultiplier * newValue.AnyMultiplier,
					};
				case ValueStackingType.MinCurrent:
					return new ResistanceAmount {
						PhysicalMultiplier = Mathf.Min(current.PhysicalMultiplier, newValue.PhysicalMultiplier),
						MagicalMultiplier = Mathf.Min(current.MagicalMultiplier, newValue.MagicalMultiplier),
						AnyMultiplier = Mathf.Min(current.AnyMultiplier, newValue.AnyMultiplier),
					};
				case ValueStackingType.MaxCurrent:
					return new ResistanceAmount {
						PhysicalMultiplier = Mathf.Max(current.PhysicalMultiplier, newValue.PhysicalMultiplier),
						MagicalMultiplier = Mathf.Max(current.MagicalMultiplier, newValue.MagicalMultiplier),
						AnyMultiplier = Mathf.Max(current.AnyMultiplier, newValue.AnyMultiplier),
					};
				case ValueStackingType.AddOriginal:
					return new ResistanceAmount {
						PhysicalMultiplier = original.PhysicalMultiplier + newValue.PhysicalMultiplier,
						MagicalMultiplier = original.MagicalMultiplier + newValue.MagicalMultiplier,
						AnyMultiplier = original.AnyMultiplier + newValue.AnyMultiplier,
					};
				case ValueStackingType.MultiplyOriginal:
					return new ResistanceAmount {
						PhysicalMultiplier = original.PhysicalMultiplier * newValue.PhysicalMultiplier,
						MagicalMultiplier = original.MagicalMultiplier * newValue.MagicalMultiplier,
						AnyMultiplier = original.AnyMultiplier * newValue.AnyMultiplier,
					};
				case ValueStackingType.MinOriginal:
					return new ResistanceAmount {
						PhysicalMultiplier = Mathf.Min(original.PhysicalMultiplier, newValue.PhysicalMultiplier),
						MagicalMultiplier = Mathf.Min(original.MagicalMultiplier, newValue.MagicalMultiplier),
						AnyMultiplier = Mathf.Min(original.AnyMultiplier, newValue.AnyMultiplier),
					};
				case ValueStackingType.MaxOriginal:
					return new ResistanceAmount {
						PhysicalMultiplier = Mathf.Max(original.PhysicalMultiplier, newValue.PhysicalMultiplier),
						MagicalMultiplier = Mathf.Max(original.MagicalMultiplier, newValue.MagicalMultiplier),
						AnyMultiplier = Mathf.Max(original.AnyMultiplier, newValue.AnyMultiplier),
					};
				default:
					return newValue;
			}
		}
	}
}