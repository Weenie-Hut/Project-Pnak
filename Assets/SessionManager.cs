using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pnak
{
	[RequireComponent(typeof(NetworkRunner))]
	public class SessionManager : SingletonMono<SessionManager>, INetworkRunnerCallbacks
	{
		public NetworkRunner NetworkRunner { get; private set; }

		protected override void Awake()
		{
			base.Awake();

			NetworkRunner = GetComponent<NetworkRunner>();
			NetworkRunner.ProvideInput = true;
		}

		public void StartGame(int sceneIndex, GameMode mode)
		{
			GameManager.Instance.SceneLoader.LoadSceneAsync(sceneIndex, _ => Connect(sceneIndex, mode));
		}

		private async void Connect(int sceneIndex, GameMode mode)
		{
			await NetworkRunner.StartGame(new StartGameArgs()
			{
				GameMode = mode, 
				// SessionName = "TestRoom", 
				Scene = sceneIndex,
				SceneManager = GameManager.Instance.SceneLoader
			});
		}

		public int PlayerCount => NetworkRunner.ActivePlayers.Count();
		public NetworkObject LocalPlayer => NetworkRunner.GetPlayerObject(NetworkRunner.LocalPlayer);

		[SerializeField] private NetworkPrefabRef _characterPrefab;
		private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			if (runner.IsServer)
			{
				Vector3 spawnPosition = new Vector3((player.RawEncoded%runner.Config.Simulation.DefaultPlayers)*200 - 300,0,0);
				NetworkObject networkPlayerObject = runner.Spawn(_characterPrefab, spawnPosition, Quaternion.identity, player, (_, __) => {
				});
				MessageBox.Instance.ShowMessage($"Player {player} has joined the game!");
				_spawnedCharacters.Add(player, networkPlayerObject);
			}
		}

		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
			if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
			{
				runner.Despawn(networkObject);
				_spawnedCharacters.Remove(player);
			}
		}

		public void OnInput(NetworkRunner runner, NetworkInput input)
		{
			input.Set(GameManager.Instance.PullNetworkInput());
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}
}