using UnityEngine;
using System.Linq;

namespace Pnak
{
	public class GameManager : SingletonMono<GameManager>
	{
		[Tooltip("The character data to use for each character type. Temporary until we have character prefabs.")]
		public CharacterTypeRadialOption[] CharacterOptions;

		private PlayerAgent[] _characters;
		public PlayerAgent[] Characters => _characters ?? (_characters = CharacterOptions.Select(o => o.AgentPrefab).ToArray());

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

			if (cost.Money > SpawnerManager.GlobalMoney)
				return false;

			return true;
		}

		internal bool Charge(Cost cost, Interactable interactable)
		{
			if (!CanAfford(cost, interactable))
				return false;

			Player.LocalPlayer.RPC_ChangeMP(-cost.MP);
			SpawnerManager.RPC_ChangeMoney(-cost.Money);
			return true;
		}
	}
}
