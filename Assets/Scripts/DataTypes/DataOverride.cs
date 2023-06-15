using UnityEngine;

namespace Pnak
{
	[System.Serializable]
	public class DataOverride<T>
	{
		[EnumNameSuffix(typeof(Priorities))]
		[Tooltip("The order in which this override is applied. The lower the number, the earlier it is applied.")]
		public ushort Priority = (ushort)Priorities.GeneralMult;
		[Tooltip("The way this override stacks with the target. You can hover over the options after selecting them to see what they do.")]
		public ValueStackingType StackingType = ValueStackingType.Add;
		[Tooltip("Data for this override.")]
		public T Data = default;
	}
}