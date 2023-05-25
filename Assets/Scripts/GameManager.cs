using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System;
using Pnak.Input;

namespace Pnak
{
	public class GameManager : SingletonMono<GameManager>
	{
		[Tooltip("The character data to use for each character type. Temporary until we have character prefabs.")]
		public CharacterData[] Characters;

		public Camera MainCamera { get; private set; }
		public SceneLoader SceneLoader { get; private set; }

		protected override void Awake()
		{
			base.Awake();

			MainCamera = Camera.main;
			DontDestroyOnLoad(MainCamera.gameObject);

			SceneLoader = FindObjectOfType<SceneLoader>();
		}

		internal bool CanAfford(Cost cost, Interactable interactable)
		{
			if (Player.LocalPlayer == null)
			{
				UnityEngine.Debug.LogWarning("Cannot afford cost because there is no local player.");
				return false;
			}

			if (cost.HP != 0)
			{
				UnityEngine.Debug.LogWarning("HP costs are not yet implemented.");
			}

			if (cost.MP > Player.LocalPlayer.MP)
				return false;

			if (cost.Money != 0)
			{
				UnityEngine.Debug.LogWarning("Money costs are not yet implemented.");
			}

			return true;
		}

		internal bool Charge(Cost cost, Interactable interactable)
		{
			if (!CanAfford(cost, interactable))
				return false;

			Player.LocalPlayer.MP_Change -= cost.MP;
			return true;
		}
	}
}
