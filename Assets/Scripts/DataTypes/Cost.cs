namespace Pnak
{
	[System.Serializable]
	public struct Cost
	{
		public float MP;
		public float HP;
		public float Money;

		public override string ToString()
		{
			string result =
				  (MP > 0 ? $" {MP} MP" : "")
				+ (HP > 0 ? $" {HP} HP" : "")
				+ (Money > 0 ? $" ${Money}" : "");

			if (result.Length == 0)
			{
				result = "Free";
			}
			else result = result.Substring(1);

			return result;
		}
	}
}