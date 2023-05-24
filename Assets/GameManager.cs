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
	}
}
