using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Player : NetworkBehaviour
	{
		public static Player LocalPlayer { get; private set; }

		[SerializeField] private Transform _AimGraphic;

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

		public override void FixedUpdateNetwork()
		{
			if (GetInput(out NetworkInputData input))
			{
				if (input.CurrentInputMap == Input.InputMap.Menu)
				{
					byte nextType = CharacterType;
					if (input.GetButtonPressed(1)) nextType = 1;
					if (input.GetButtonPressed(2)) nextType = 2;
					if (input.GetButtonPressed(3)) nextType = 3;
					if (input.GetButtonPressed(4)) nextType = 4;

					if (nextType != CharacterType)
					{
						CharacterType = nextType;
						
						reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
						towerDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.TowerPlacementTime);
					}

					return;
				}

				if (!PlayerLoaded) return;

				Vector2 movement = input.Movement * CurrentCharacterData.Speed;
				transform.position += (Vector3)movement * Runner.DeltaTime;

				float _rotation = input.AimAngle;

				if (reloadDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.GetButtonDown(1))
					{
						reloadDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.ReloadTime);
						Runner.Spawn(CurrentCharacterData.ProjectilePrefab, transform.position, Quaternion.Euler(0.0f, 0.0f, _rotation), Object.InputAuthority);
					}
				}

				if (towerDelay.ExpiredOrNotRunning(Runner))
				{
					if (input.GetButtonPressed(2))
					{
						towerDelay = TickTimer.CreateFromSeconds(Runner, CurrentCharacterData.TowerPlacementTime);
						Runner.Spawn(CurrentCharacterData.TowerPrefab, transform.position, Quaternion.identity, Object.InputAuthority, (runner, o) =>
						{
							o.GetComponent<Tower>().Init(_rotation);
						});
					}
				}
			}
		}

		public override void Render()
		{
			if (!PlayerLoaded) return;
			if (!Object.HasInputAuthority) return;

			if (LevelUI.Exists)
			{
				float? reloadTime = reloadDelay.RemainingTime(Runner);
				LevelUI.Instance.ShootReloadBar.Value = reloadTime.HasValue ? (1 - reloadTime.Value / CurrentCharacterData.ReloadTime) : 1.0f;
				float? towerTime = towerDelay.RemainingTime(Runner);
				LevelUI.Instance.TowerReloadBar.Value = towerTime.HasValue ? (1 - towerTime.Value / CurrentCharacterData.TowerPlacementTime) : 1.0f;
			}

			_AimGraphic.rotation = Quaternion.Euler(0.0f, 0.0f, Input.GameInput.Instance.InputData.AimAngle);
		}

		public override void Spawned()
		{
			if (!Object.HasInputAuthority)
			{
				_AimGraphic.gameObject.SetActive(false);
				return;
			}
			
			if (LocalPlayer != null)
			{
				Debug.LogError("Multiple local players detected!");
				return;
			}
			LocalPlayer = this;

			GameManager.Instance.SceneLoader.FinishedLoading();
			
		}

		private void SetCharacterType(byte characterType)
		{
			UnityEngine.Debug.Log("Setting character type to " + characterType);
			CharacterType = characterType;
		}

		public static void OnCharacterTypeChanged(Changed<Player> changed) => changed.Behaviour.ChangeCharacterSprite();

		private void ChangeCharacterSprite()
		{
			MessageBox.Instance.RPC_ShowMessage("Player changed character to " + CurrentCharacterData.Name + "!");

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