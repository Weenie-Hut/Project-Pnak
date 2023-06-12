using System.Collections;
using System.Collections.Generic;

namespace Pnak
{
	[System.Serializable]
	public struct Cost : Stackable<Cost>, Copyable<Cost>
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

		public Cost Copy()
		{
			return new Cost()
			{
				MP = MP,
				HP = HP,
				Money = Money
			};
		}

		public void StackWith(Cost other, ValueStackingType stackingType)
		{
			MP = ValueStack.Stack(MP, other.MP, stackingType);
			HP = ValueStack.Stack(HP, other.HP, stackingType);
			Money = ValueStack.Stack(Money, other.Money, stackingType);
		}

		public static Cost Zero => new Cost() { MP = 0, HP = 0, Money = 0 };
	}
}