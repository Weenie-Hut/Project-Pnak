using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace Pnak
{
	public enum ValueStackingType
	{
		[Tooltip("This value cannot be stacked. Only works if all values will not stack (new modifiers will be created instead), otherwise treated like keep current. Ex: Adding DoT that independently expires, but multiple DoTs can be applied.")]
		DoNotStack,

		[Tooltip("When stacking, this field will be ignored.")]
		Keep,
		[Tooltip("When stacking, this field will be replaced with the new value.")]
		Replace,

		[Tooltip("When stacking, this field will be added to the current value.")]
		Add,
		[Tooltip("When stacking, this field will be subtracted from the current value.")]
		Subtract,
		[Tooltip("When stacking, this field will be multiplied by the current value.")]
		Multiply,
		[Tooltip("When stacking, this field will be divided by the current value.")]
		Divide,

		Min,
		Max,
	}

	[System.Serializable]
	public struct StackSettings
	{
		public ValueStackingType StackingType;
		public int StackId;
	}

	[System.Serializable]
	public struct ValueStackSettings<T>
	{
		public ValueStackingType StackingType;
		public int StackId;
		public T Value;

		public static implicit operator T(ValueStackSettings<T> settings) => settings.Value;
	}

	public static partial class ValueStack
	{
		public static List<T> Stack<T>(List<T> current, List<T> incoming, ValueStackingType stackingType)
			=> StackEnumerable(current, incoming, stackingType).ToList();

		public static T[] Stack<T>(T[] current, T[] incoming, ValueStackingType stackingType)
			=> StackEnumerable(current, incoming, stackingType).ToArray();

		public static T Stack<T>(T current, T incoming, ValueStackingType stackingType) where T : UnityEngine.Object
			=> StackReference(current, incoming, stackingType);

		public static float Stack(float current, float incoming, ValueStackingType stackingType)
		{
			if (float.IsNaN(incoming))
				return current;

			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current;
				case ValueStackingType.Replace:
					return incoming;
				case ValueStackingType.Add:
					return current + incoming;
				case ValueStackingType.Subtract:
					return current - incoming;
				case ValueStackingType.Multiply:
					return current * incoming;
				case ValueStackingType.Divide:
					return current / incoming;
				case ValueStackingType.Min:
					return Mathf.Min(current, incoming);
				case ValueStackingType.Max:
					return Mathf.Max(current, incoming);
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}

		public static int Stack(int current, int incoming, ValueStackingType stackingType)
		{
			if (incoming == int.MinValue)
				return current;

			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current;
				case ValueStackingType.Replace:
					return incoming;
				case ValueStackingType.Add:
					return current + incoming;
				case ValueStackingType.Subtract:
					return current - incoming;
				case ValueStackingType.Multiply:
					return current * incoming;
				case ValueStackingType.Divide:
					return current / incoming;
				case ValueStackingType.Min:
					return Mathf.Min(current, incoming);
				case ValueStackingType.Max:
					return Mathf.Max(current, incoming);
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}

		public static Vector2 Stack(Vector2 current, Vector2 incoming, ValueStackingType stackingType)
		{
			if (float.IsNaN(incoming.x) || float.IsNaN(incoming.y))
				return current;

			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current;
				case ValueStackingType.Replace:
					return incoming;
				case ValueStackingType.Add:
					return current + incoming;
				case ValueStackingType.Subtract:
					return current - incoming;
				case ValueStackingType.Multiply:
					return new Vector2(current.x * incoming.x, current.y * incoming.y);
				case ValueStackingType.Divide:
					return new Vector2(current.x / incoming.x, current.y / incoming.y);
				case ValueStackingType.Min:
					return new Vector2(Mathf.Min(current.x, incoming.x), Mathf.Min(current.y, incoming.y));
				case ValueStackingType.Max:
					return new Vector2(Mathf.Max(current.x, incoming.x), Mathf.Max(current.y, incoming.y));
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}

		public static Vector3 Stack(Vector3 current, Vector3 incoming, ValueStackingType stackingType)
		{
			if (float.IsNaN(incoming.x) || float.IsNaN(incoming.y) || float.IsNaN(incoming.z))
				return current;

			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current;
				case ValueStackingType.Replace:
					return incoming;
				case ValueStackingType.Add:
					return current + incoming;
				case ValueStackingType.Subtract:
					return current - incoming;
				case ValueStackingType.Multiply:
					return new Vector3(current.x * incoming.x, current.y * incoming.y, current.z * incoming.z);
				case ValueStackingType.Divide:
					return new Vector3(current.x / incoming.x, current.y / incoming.y, current.z / incoming.z);
				case ValueStackingType.Min:
					return new Vector3(Mathf.Min(current.x, incoming.x), Mathf.Min(current.y, incoming.y), Mathf.Min(current.z, incoming.z));
				case ValueStackingType.Max:
					return new Vector3(Mathf.Max(current.x, incoming.x), Mathf.Max(current.y, incoming.y), Mathf.Max(current.z, incoming.z));
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}

		private static T StackReference<T>(T current, T incoming, ValueStackingType stackingType) where T : class
		{
			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current;
				case ValueStackingType.Replace:
					return incoming;
				case ValueStackingType.Add:
				case ValueStackingType.Multiply:
				case ValueStackingType.Max:
					return incoming ?? current;
				case ValueStackingType.Subtract:
				case ValueStackingType.Divide:
					return incoming.Equals(current) ? null : current;
				case ValueStackingType.Min:
					return incoming == null ? incoming : current;
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}

		public static IEnumerable<T> StackEnumerable<T>(IEnumerable<T> current, IEnumerable<T> incoming, ValueStackingType stackingType)
		{
			switch (stackingType)
			{
				case ValueStackingType.DoNotStack:
					throw new System.ArgumentException("Cannot stack value with DoNotStack type.");
				case ValueStackingType.Keep:
					return current ?? new T[0];
				case ValueStackingType.Replace:
					return incoming ?? new T[0];
				case ValueStackingType.Add:
				case ValueStackingType.Multiply:
					return current?.Concat(incoming) ?? incoming ?? new T[0];
				case ValueStackingType.Min:
				case ValueStackingType.Subtract:
				case ValueStackingType.Divide:
					return current?.Except(incoming) ?? new T[0];
				case ValueStackingType.Max:
					return current?.Intersect(incoming) ?? new T[0];
				default:
					throw new System.ArgumentException("Unknown stacking type.");
			}
		}
	}
}