using Fusion;
using UnityEngine;
using System.Collections.Generic;

namespace Pnak
{
	public class DamageMunition : Munition
	{
		[Tooltip("Damage dealt to each target hit, in order.")]
		public List<DamageAmount> DamageByPeirce = new List<DamageAmount> { new DamageAmount { PureDamage = 1f } };
		[Tooltip("If true, the projectile will despawn when it hits targets equal to the length of DamageByPeirce. If false, the projectile will keep looping through DamageByPeirce.")]
		public bool CappedPeirce = true;
		[Tooltip("The number of targets the projectile can hit before despawning. If CappedPeirce is false, this value is ignored.")]
		public int Peirce = 1;
		[Tooltip("If true, the projectile will ignore targets after the first hit. Disable for DoT while colliding effects.")]
		public bool IgnoreAfterFirstHit = true;

		public int PeirceCount { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			PeirceCount = 0;
		}

		protected override void OnHit(Collider2D collider2D, float? distance)
		{
			if (CappedPeirce && PeirceCount >= Peirce)
				return; // Already hit max number of targets. Wait for despawn

			DamageAmount damage = DamageByPeirce[PeirceCount];
			if (!IgnoreAfterFirstHit)
				damage *= Runner.DeltaTime;

			CollisionProcessor.ApplyDamage(collider2D, damage);

			PeirceCount++;
			if (CappedPeirce)
			{
				if (PeirceCount >= Peirce)
				{
					Controller.QueueForDestroy();
					return;
				}
			}
			else if (PeirceCount >= Peirce)
			{
				PeirceCount = 0;
			}

			if (IgnoreAfterFirstHit)
				CollisionProcessor.IgnoreCollider(collider2D);
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(DamageMunition))]
	public class DamageMunitionEditor : UnityEditor.Editor
	{
		private UnityEditor.SerializedProperty _DamageByPeirce;
		private UnityEditor.SerializedProperty _CappedPeirce;
		private UnityEditor.SerializedProperty _Peirce;
		private UnityEditor.SerializedProperty _IgnoreAfterFirstHit;

		private void OnEnable()
		{
			_DamageByPeirce = serializedObject.FindProperty("DamageByPeirce");
			_CappedPeirce = serializedObject.FindProperty("CappedPeirce");
			_Peirce = serializedObject.FindProperty("Peirce");
			_IgnoreAfterFirstHit = serializedObject.FindProperty("IgnoreAfterFirstHit");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUILayout.PropertyField(_DamageByPeirce, true);
			UnityEditor.EditorGUILayout.PropertyField(_CappedPeirce);
			if (_CappedPeirce.boolValue)
				UnityEditor.EditorGUILayout.PropertyField(_Peirce);
			UnityEditor.EditorGUILayout.PropertyField(_IgnoreAfterFirstHit);

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}