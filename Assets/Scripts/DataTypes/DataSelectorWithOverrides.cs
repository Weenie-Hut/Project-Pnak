using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace Pnak
{
	[System.Serializable]
	public class DataSelector<T> : Copyable<DataSelector<T>> where T : Stackable<T>, Copyable<T>
	{
		[Tooltip("Initial firing delays. Whenever reload times are going to be set, the first value in this list will be removed and used if one exits.")]
		public List<WeightedOption<T>> TemporaryData = new List<WeightedOption<T>>();

		[Tooltip("Possible Damage Data. If " + nameof(RandomizeDataOrder) + " is true, data is picked randomly after each hit. If data has a weight of 'NaN', it will always come after the previous entry regardless of randomness. Otherwise, damage data is picked in order they appear.")]
		public List<WeightedOption<T>> NormalData = new List<WeightedOption<T>>();

		[Tooltip("If true, the damage data will be picked at random using weights, after temporary damage data has been used. Otherwise, the damage data will be picked in order, cyclicly, based on pierce.")]
		public bool RandomizeDataOrder = false;

		protected int lastNormalIndex = -1;
		protected int currentDataIndex = int.MinValue;
		public T CurrentData { get; protected set; }

		public bool MoveToNext()
		{
			bool changed = false;
			if (currentDataIndex != int.MinValue)
			{
				if (currentDataIndex == -1 && TemporaryData.Count > 0)
				{
					TemporaryData.RemoveAt(0);
					changed = true;
				}
			}

			if (TemporaryData.Count > 0)
			{
				if (RandomizeDataOrder)
				{
					int pick = WeightedOption<T>.PickIndex(TemporaryData);
					// Swap the picked index with the first index
					T temp = TemporaryData[0];
					TemporaryData[0] = TemporaryData[pick];
					TemporaryData[pick] = temp;
				}

				currentDataIndex = -1;
				changed = true;
			}
			else
			{
				int nextCyclicIndex = (lastNormalIndex + 1) % NormalData.Count;

				bool pickRandom = RandomizeDataOrder;

				// If the next weight is 0, move to the next index instead of picking randomly
				if (pickRandom && lastNormalIndex >= 0)
					pickRandom = NormalData[nextCyclicIndex].Weight != 0;

				if (pickRandom) {
					currentDataIndex = WeightedOption<T>.PickIndex(NormalData);
				}
				else {
					currentDataIndex = nextCyclicIndex;
				}

				if (lastNormalIndex != currentDataIndex)
					changed = true;
				lastNormalIndex = currentDataIndex;
			}

			if (changed)
				SetData();
			
			return changed;
		}

		protected virtual void SetData()
		{
			UnityEngine.Debug.Assert(currentDataIndex != int.MinValue, $"Data from ({GetType().Name}) is being set without being moved to first.");
			UnityEngine.Debug.Assert(currentDataIndex < NormalData.Count || currentDataIndex == -1, $"Data from ({GetType().Name}) has an invalid index {currentDataIndex}. This is likely due to there not being data available in either list.");

			T data = currentDataIndex != -1 ? NormalData[currentDataIndex] : TemporaryData[0];

			UnityEngine.Debug.Assert(data != null, $"Data from ({GetType().Name}) has no data for index {currentDataIndex} and no temporary");

			CurrentData = data.Copy();
		}

		public DataSelector<T> Copy()
		{
			DataSelector<T> copy = new DataSelector<T>();
			copy.TemporaryData = new List<WeightedOption<T>>(TemporaryData);
			copy.NormalData = new List<WeightedOption<T>>(NormalData);
			copy.RandomizeDataOrder = RandomizeDataOrder;
			copy.currentDataIndex = int.MaxValue;
			return copy;
		}
	}

	/// <summary>
	/// Comparer for comparing two keys, handling equality as beeing greater
	/// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
	/// </summary>
	public class DuplicateKeyComparer<T> : IComparer<DataOverride<T>> where T : Stackable<T>, Copyable<T>
	{
		public int Compare(DataOverride<T> x, DataOverride<T> y)
		{
			int result = x.Priority.CompareTo(y.Priority);

			if (result == 0) return x.GetHashCode().CompareTo(y.GetHashCode());
			else return result;
		}
	}

	public interface Overridable<T> where T : Stackable<T>, Copyable<T>
	{
		public void AddOverride(DataOverride<T> data);
		public void RemoveOverride(DataOverride<T> data);
		public void ModifyOverride(DataOverride<T> data);
	}

	[System.Serializable]
	public class DataSelectorWithOverrides<T> : DataSelector<T>, Overridable<T> where T : Stackable<T>, Copyable<T>
	{
		private SortedSet<DataOverride<T>> DataOverrides = new SortedSet<DataOverride<T>>(new DuplicateKeyComparer<T>());
		public SortedSet<DataOverride<T>> DataOverridesEnumerator => DataOverrides;

		protected override void SetData()
		{
			UnityEngine.Debug.Assert(currentDataIndex != int.MinValue, $"Data from ({GetType().Name}) is being set without being moved to first.");
			UnityEngine.Debug.Assert(currentDataIndex < NormalData.Count || (currentDataIndex == -1 && TemporaryData.Count > 0), $"Data from ({GetType().Name}) has an invalid index {currentDataIndex}. This is likely due to there not being data available in either list.");

			T data = currentDataIndex != -1 ? NormalData[currentDataIndex] : TemporaryData[0];

			UnityEngine.Debug.Assert(data != null, $"Data from ({GetType().Name}) has no data for index {currentDataIndex} and no temporary");

			data = data.Copy();
			foreach (DataOverride<T> overrides in DataOverrides)
			{
				data.StackWith(overrides.Data, overrides.StackingType);
			}

			CurrentData = data;
		}

		public void AddOverride(DataOverride<T> data)
		{
			DataOverrides.Add(data);
			SetData();
		}

		public void RemoveOverride(DataOverride<T> data)
		{
			DataOverrides.Remove(data);
			SetData();
		}

		public void ModifyOverride(DataOverride<T> data)
		{
			DataOverrides.Remove(data);
			DataOverrides.Add(data);
			SetData();
		}
	}
}