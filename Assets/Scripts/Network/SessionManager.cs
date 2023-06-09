using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace Pnak
{
	[RequireComponent(typeof(NetworkRunner))]
	public class SessionManager : SingletonMono<SessionManager>, INetworkRunnerCallbacks
	{
		public const float DeltaTime = 1f / 60f;
		public NetworkRunner NetworkRunner { get; private set; }
		public static bool IsServer => Instance?.NetworkRunner?.IsServer ?? false;
		public static PlayerRef LocalPlayer => Instance?.NetworkRunner?.LocalPlayer ?? PlayerRef.None;
		public static int Tick => Instance?.NetworkRunner?.Tick ?? -1;
		public static bool HasExpired(int startTick, float duration)
		{
			float endTick = startTick + (duration / Instance.NetworkRunner.DeltaTime);
			return Instance.NetworkRunner.Tick >= endTick;
		}

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

		[SerializeField] private NetworkPrefabRef _characterPrefab;
		private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			if (runner.IsServer)
			{
				Vector3 spawnPosition = new Vector3((player.RawEncoded%runner.Config.Simulation.DefaultPlayers)*200 - 300,0,0);
				NetworkObject networkPlayerObject = runner.Spawn(_characterPrefab, spawnPosition, Quaternion.identity, player, (_, __) => {
				});
				MessageBox.Instance.RPC_ShowMessage($"Player {player} has joined the game!");
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
			input.Set(Input.GameInput.Instance.PullNetworkInput());
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnConnectedToServer(NetworkRunner runner)
		{
			UnityEngine.Debug.Assert(DeltaTime == runner.DeltaTime, $"DeltaTime {DeltaTime} must be the same as NetworkRunner.DeltaTime {runner.DeltaTime}!");
		}
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