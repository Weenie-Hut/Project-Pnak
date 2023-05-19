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
		[SerializeField] private Bullet _prefabBullet;
		[SerializeField] private Tower _prefabTower;

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
					angle = Mathf.Atan2(input.movement.y, input.movement.x) * Mathf.Rad2Deg;
				}

				if (reloadDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.GetButton(0))
					{
						reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
						UnityEngine.Debug.Log("Firing bullet at angle: " + angle + " (x: " + input.movement.x + ", y: " + input.movement.y + ")");
						Runner.Spawn(_prefabBullet, transform.position, Quaternion.Euler(0.0f, 0.0f, angle), Object.InputAuthority, (runner, o) =>
						{
							o.GetComponent<Bullet>().Init();
						});
					}
				}

				if (towerDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.GetButton(1))
					{
						towerDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.TowerPlacementTime);
						Runner.Spawn(_prefabTower, transform.position, Quaternion.Euler(0.0f, 0.0f, angle), Object.InputAuthority, (runner, o) =>
						{
							o.GetComponent<Tower>().Init(CurrentCharacterData.TowerReloadTime, CurrentCharacterData.TowerLifetime);
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

		private KeyValuePair<GameManager.Buttons, Action>[] _buttonActions;

		private void Start()
		{
			// if (!Object.HasInputAuthority) yield break;

			_buttonActions = new KeyValuePair<GameManager.Buttons, Action>[] {
				new (GameManager.Buttons.MenuButton_1, () => SetCharacterType(1)),
				new (GameManager.Buttons.MenuButton_2, () => SetCharacterType(2)),
				new (GameManager.Buttons.MenuButton_3, () => SetCharacterType(3)),
				new (GameManager.Buttons.MenuButton_4, () => SetCharacterType(4))
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