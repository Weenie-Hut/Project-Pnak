using UnityEngine;

namespace Pnak
{

	public static class MathUtil
	{
		public static Vector2 AngleToDirection(float angle)
		{
			return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
		}
	}

}