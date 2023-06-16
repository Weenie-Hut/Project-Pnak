using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Pnak
{
	[System.Serializable]
	public class ShootData : Formatable, Copyable<ShootData>, Stackable<ShootData>
	{
		[NaNButton, Suffix("sec"), Tooltip("The time it takes to reload the weapon. Format using {reloadTime}."), MinMax(min: 0f)]
		public float ReloadTime = 1f;
		[NaNButton, Tooltip("The range of shots to fire. Format using {fireCountRange}."), Suffix("shots"), MinMax(min: 0f)]
		public Vector2 FireCountRange = new Vector2(1, 1);
		[Tooltip("The maximum angle for a shot. 0 means all shots will be in the same direction. 180 means all shots will be in random directions. Format using {fireSpreadAngle}."), Suffix("deg"), MinMax(0f, 360f), NaNButton]
		public float FireSpreadAngle = 1f;
		[Tooltip("Format using {spawn}.")]
		public StateBehaviourController Spawn = null;
		[Tooltip("The mods that are applied to the munition.")]
		public SerializedLiteNetworkedData[] MunitionMods = new SerializedLiteNetworkedData[0];
		[Tooltip("The mods that are added to the munition as damage overrides.")]
		public DataOverride<DamageAmount>[] DamageMods = new DataOverride<DamageAmount>[0];


		public string Format(string format)
		{
			return format.FormatById("reloadTime", ReloadTime)
				.FormatById("fireCountRange", $"{FireCountRange.x} - {FireCountRange.y}")
				.FormatById("fireSpreadAngle", FireSpreadAngle)
				.FormatById("spawn", Spawn);
		}

		public ShootData Copy()
		{
			return new ShootData
			{
				ReloadTime = ReloadTime,
				FireCountRange = FireCountRange,
				FireSpreadAngle = FireSpreadAngle,
				Spawn = Spawn,
				MunitionMods = MunitionMods,
				DamageMods = DamageMods
			};
		}

		public void StackWith(ShootData other, ValueStackingType stackingType)
		{
			ValueStack.StackInPlace(this, other, stackingType);
		}
	}

	public static partial class ValueStack
	{
		public static void StackInPlace(ShootData a, ShootData b, ValueStackingType stackingType)
		{
			a.ReloadTime = Stack(a.ReloadTime, b.ReloadTime, stackingType);
			a.FireCountRange = Stack(a.FireCountRange, b.FireCountRange, stackingType);
			a.FireSpreadAngle = Stack(a.FireSpreadAngle, b.FireSpreadAngle, stackingType);
			a.Spawn = Stack(a.Spawn, b.Spawn, stackingType);
			a.MunitionMods = Stack(a.MunitionMods, b.MunitionMods, stackingType);
			a.DamageMods = Stack(a.DamageMods, b.DamageMods, stackingType);
		}

		public static ShootData Stack(ShootData a, ShootData b, ValueStackingType stackingType)
		{
			ShootData result = a.Copy();
			StackInPlace(result, b, stackingType);
			return result;
		}
	}
}