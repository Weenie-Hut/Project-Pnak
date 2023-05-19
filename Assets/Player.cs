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

		[Networked(OnChanged = nameof(OnCharacterTypeChanged))]
		public byte CharacterType { get; private set; }

		public override void FixedUpdateNetwork()
		{
			if (GetInput(out NetworkInputData input))
			{
				transform.position += (Vector3)input.movement * Runner.DeltaTime * GameManager.Instance.Characters[CharacterType].Speed;
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

			while (!GameManager.Exists)
				yield return null;

			foreach (var buttonAction in _buttonActions)
				GameManager.Instance.AddButtonListener(buttonAction.Key, buttonAction.Value);
		}

		private void OnDestroy()
		{
			if (GameManager.Exists)
				foreach (var buttonAction in _buttonActions)
					GameManager.Instance.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

		private void SetCharacterType(byte characterType)
		{
			CharacterType = characterType;
		}

		public static void OnCharacterTypeChanged(Changed<Player> changed) => changed.Behaviour.ChangeCharacterSprite();

		private void ChangeCharacterSprite()
		{
			CharacterRenderer.sprite = GameManager.Instance.Characters[CharacterType].Sprite;
			CharacterText.text = GameManager.Instance.Characters[CharacterType].Name;
			CharacterRenderer.transform.localScale = (Vector3)GameManager.Instance.Characters[CharacterType].SpriteScale;
			CharacterRenderer.transform.localPosition = (Vector3)GameManager.Instance.Characters[CharacterType].SpritePosition;
		}
	}
}