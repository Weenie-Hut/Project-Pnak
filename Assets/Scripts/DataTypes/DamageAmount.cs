namespace Pnak
{
	[System.Serializable]
	public struct DamageAmount
	{
		public float PhysicalDamage;
		public float MagicalDamage;
		public float PureDamage;
		
		public static DamageAmount operator *(DamageAmount damage, float multiplier)
		{
			return new DamageAmount
			{
				PhysicalDamage = damage.PhysicalDamage * multiplier,
				MagicalDamage = damage.MagicalDamage * multiplier,
				PureDamage = damage.PureDamage * multiplier
			};
		}
	}

	
}