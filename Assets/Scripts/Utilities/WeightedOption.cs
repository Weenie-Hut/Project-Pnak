using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	
	[System.Serializable]
	public struct WeightedOption<T>
	{
		public T Option;
		[Tooltip("The weight of this data when randomly picking  data to use. Temporary shoot data will always be picked before regular shoot data, but will use this random pick weight if multiple temporary data exist.")]
		[Suffix("weight"), Min(0)] public float Weight;

		public WeightedOption(T option, float weight)
		{
			Option = option;
			Weight = weight;
		}

		public static implicit operator T(WeightedOption<T> option)
		{
			return option.Option;
		}

		public static implicit operator WeightedOption<T>(T option)
		{
			return new WeightedOption<T>(option, 1.0f);
		}

		public bool TryAdd(WeightedOption<T> other, out WeightedOption<T> result)
		{
			if (Option.Equals(other.Option))
			{
				result = new WeightedOption<T>(Option, Weight + other.Weight);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		public bool TryRemove(WeightedOption<T> other, out WeightedOption<T> result)
		{
			if (Option.Equals(other.Option))
			{
				if (Weight - other.Weight < -0.001f)
				{
					Debug.LogWarning("WeightedOption weight is negative when removing weight. This should not happen.");
				}

				result = new WeightedOption<T>(Option, Mathf.Max(Weight - other.Weight, 0.0f));

				

				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		public bool TryMult(WeightedOption<T> other, out WeightedOption<T> result)
		{
			if (Option.Equals(other.Option))
			{
				result = new WeightedOption<T>(Option, Weight * other.Weight);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		public bool TryDiv(WeightedOption<T> other, out WeightedOption<T> result)
		{
			if (Option.Equals(other.Option))
			{
				result = new WeightedOption<T>(Option, Mathf.Max(Weight / other.Weight, 0.0f));
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		public static int PickIndex(List<WeightedOption<T>> list)
		{
			if (list == null)
				return -1;

			if (list.Count == 0)
				return -1;

			if (list.Count == 1)
				return 0;

			float totalWeight = 0.0f;
			for (int i = 0; i < list.Count; i++)
			{
				if (float.IsNaN(list[i].Weight))
					continue;

				UnityEngine.Debug.Assert(list[i].Weight >= 0.0f, "WeightedOption weight is negative when picking index. This should not happen.");
				totalWeight += list[i].Weight;
			}

			float random = Random.Range(0.0f, totalWeight);
			for (int i = 0; i < list.Count; i++)
			{
				if (float.IsNaN(list[i].Weight))
					continue;

				random -= list[i].Weight;
				if (random <= 0.0f)
				{
					return i;
				}
			}

			Debug.LogWarning("WeightedOption list is empty when picking index. This should not happen.");
			return -1;
		}

		public static T PickOption(List<WeightedOption<T>> list)
		{
			int index = PickIndex(list);
			if (index == -1)
				return default;

			return list[index];
		}
	}
}