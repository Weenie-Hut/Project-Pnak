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
		public byte CharacterType { get; private set; }

		public bool PlayerLoaded => CharacterType != 0;
		public CharacterData CurrentCharacterData => PlayerLoaded ? GameManager.Instance.Characters[CharacterType - 1] : LoadingData;

		[Networked] private TickTimer reloadDelay { get; set; }
		[Networked] private TickTimer towerDelay { get; set; }

		private float angle = 0.0f;
		public override void FixedUpdateNetwork()
		{
			if (GetInput(out NetworkInputData input))
			{
				if (input.ControllerConfig == ControllerConfig.Menu)
				{
					if (input.Button3Pressed) CharacterType = 1;
					if (input.Button4Pressed) CharacterType = 2;
					if (input.Button5Pressed) CharacterType = 3;
					if (input.Button6Pressed) CharacterType = 4;

					reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
					towerDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.TowerPlacementTime);
				}

				if (!PlayerLoaded) return;

				Vector2 movement = input.movement * CurrentCharacterData.Speed;

				transform.position += (Vector3)movement * Runner.DeltaTime;

				if (movement != Vector2.zero)
				{
					angle = Mathf.Atan2(input.movement.y, input.movement.x) * Mathf.Rad2Deg;
				}

				if (reloadDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.Button1Pressed)
					{
						reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
						Runner.Spawn(CurrentCharacterData.ProjectilePrefab, transform.position, Quaternion.Euler(0.0f, 0.0f, angle), Object.InputAuthority);
					}
				}

				if (towerDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.Button2Pressed)
					{
						towerDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.TowerPlacementTime);
						float _rotation = angle;
						Runner.Spawn(CurrentCharacterData.TowerPrefab, transform.position, Quaternion.identity, Object.InputAuthority, (runner, o) =>
						{
							o.GetComponent<Tower>().Init(_rotation);
						});
					}
				}
			}
		}

		private void Update()
		{
			if (!PlayerLoaded) return;
			if (!Object.HasInputAuthority) return;
			if (!LevelUI.Exists) return;

			float? reloadTime = reloadDelay.RemainingTime(Runner);
			LevelUI.Instance.ShootReloadBar.Value = reloadTime.HasValue ? (1 - reloadTime.Value / CurrentCharacterData.ReloadTime) : 1.0f;
			float? towerTime = towerDelay.RemainingTime(Runner);
			LevelUI.Instance.TowerReloadBar.Value = towerTime.HasValue ? (1 - towerTime.Value / CurrentCharacterData.TowerPlacementTime) : 1.0f;
		}

		// private KeyValuePair<GameManager.Buttons, Action>[] _buttonActions;

		private void Start()
		{
			// if (!Object.HasInputAuthority) yield break;

			// _buttonActions = new KeyValuePair<GameManager.Buttons, Action>[] {
			// };

			// foreach (var buttonAction in _buttonActions)
			// 	GameManager.Instance.AddButtonListener(buttonAction.Key, buttonAction.Value);
		}

		public override void Spawned()
		{
			if (!Object.HasInputAuthority) return;

			GameManager.Instance.SceneLoader.FinishedLoading();
		}

		private void OnDestroy()
		{
			// foreach (var buttonAction in _buttonActions)
			// 	GameManager.Instance?.RemoveButtonListener(buttonAction.Key, buttonAction.Value);
		}

		private void SetCharacterType(byte characterType)
		{
			UnityEngine.Debug.Log("Setting character type to " + characterType);
			CharacterType = characterType;
		}

		public static void OnCharacterTypeChanged(Changed<Player> changed) => changed.Behaviour.ChangeCharacterSprite();

		private void ChangeCharacterSprite()
		{
			MessageBox.Instance.ShowMessage("Player changed character to " + CurrentCharacterData.Name + "!");

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
			if (Application.isPlaying) return;

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