using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Player : NetworkBehaviour
	{
		[Tooltip("The character sprite renderer. Temporary until we have character prefabs.")]
		public SpriteRenderer CharacterRenderer;

		[Tooltip("The character sprite renderer. Temporary until we have character prefabs.")]
		public TMPro.TextMeshPro CharacterText;

		public CharacterData LoadingData;


		[Networked(OnChanged = nameof(OnCharacterTypeChanged))]
		public byte CharacterType { get; private set; } = byte.MaxValue;

		public bool PlayerLoaded => CharacterType != byte.MaxValue;
		public CharacterData CurrentCharacterData => PlayerLoaded ? GameManager.Instance.Characters[CharacterType] : LoadingData;

		[Networked] private TickTimer reloadDelay { get; set; }
		[SerializeField] private Bullet _prefabBullet;

		private float angle = 0.0f;
		public override void FixedUpdateNetwork()
		{
			if (GetInput(out NetworkInputData input))
			{
				if (!PlayerLoaded) return;

				Vector2 movement = input.movement * CurrentCharacterData.Speed;

				transform.position += (Vector3)movement * Runner.DeltaTime;

				if (movement != Vector2.zero)
				{
					angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
				}

				if (reloadDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.GetButton(0))
					{
						reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
						Runner.Spawn(_prefabBullet, transform.position, Quaternion.Euler(0.0f, 0.0f, angle), Object.InputAuthority, (runner, o) =>
						{
							o.GetComponent<Bullet>().Init(angle);
						});
					}
				}
			}
		}

		private KeyValuePair<GameManager.Buttons, Action>[] _buttonActions;

		private IEnumerator Start()
		{
			if (!Object.HasInputAuthority) yield break;

			_buttonActions = new KeyValuePair<GameManager.Buttons, Action>[] {
				new (GameManager.Buttons.MenuButton_1, () => SetCharacterType(0)),
				new (GameManager.Buttons.MenuButton_2, () => SetCharacterType(1)),
				new (GameManager.Buttons.MenuButton_3, () => SetCharacterType(2)),
				new (GameManager.Buttons.MenuButton_4, () => SetCharacterType(3))
			};

			foreach (var buttonAction in _buttonActions)
				GameManager.Instance.AddButtonListener(buttonAction.Key, buttonAction.Value);
		}

		public override void Spawned()
		{
			if (!Object.HasInputAuthority) return;

			GameManager.Instance.SceneLoader.FinishedLoading();
		}

		private void OnDestroy()
		{
			foreach (var buttonAction in _buttonActions)
				GameManager.Instance?.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

		private void SetCharacterType(byte characterType)
		{
			CharacterType = characterType;
		}

		public static void OnCharacterTypeChanged(Changed<Player> changed) => changed.Behaviour.ChangeCharacterSprite();

		private void ChangeCharacterSprite()
		{
			CharacterRenderer.sprite = CurrentCharacterData.Sprite;
			CharacterText.text = CurrentCharacterData.Name;
			CharacterRenderer.transform.localScale = (Vector3)CurrentCharacterData.SpriteScale;
			CharacterRenderer.transform.localPosition = (Vector3)CurrentCharacterData.SpritePosition;
		}

#if UNITY_EDITOR
		/// <summary>
		/// Sets the character information so it doesn't need to be loaded on create. Also useful for previewing.
		/// </summary>
		private void OnValidate()
		{
			if (LoadingData != null)
			{
				if (CharacterRenderer != null)
				{
					CharacterRenderer.sprite = LoadingData.Sprite;
					CharacterRenderer.transform.localScale = (Vector3)LoadingData.SpriteScale;
					CharacterRenderer.transform.localPosition = (Vector3)LoadingData.SpritePosition;
				}
				if (CharacterText != null)
					CharacterText.text = LoadingData.Name;
			}
		}
#endif
	}
}