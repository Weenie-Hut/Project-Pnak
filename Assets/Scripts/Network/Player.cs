using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Pnak.Input;
using UnityEngine;

namespace Pnak
{
	public class Player : NetworkBehaviour
	{
		public static bool IsValid => LocalPlayer?.Agent != null;
		public static Player LocalPlayer { get; private set; }
		public TransformData Transform => LocalPlayer?.Agent?.Controller.HasTransform ?? false ? LocalPlayer.Agent.Controller.TransformData : default;

		public StateBehaviourController LoadingPlayerPrefab;


		[Networked(OnChanged = nameof(OnAgentNetworkIndex))]
		private ushort AgentNetworkIndex { get; set; } = ushort.MaxValue;

		private PlayerAgent _agent = null;
		public PlayerAgent Agent
		{
			get {
				if (_agent == null)
				{
					if (AgentNetworkIndex != ushort.MaxValue)
						_agent = LiteNetworkManager.GetNetworkObject(AgentNetworkIndex)?.Target?.GetComponent<PlayerAgent>();
				}
				return _agent;
			}
		}

		public bool PlayerLoaded => Agent != null;

		[Networked] private float _MP { get; set; }
		[Networked(OnChanged = nameof(OnPilotChanged))]
		private bool _Piloting { get; set; }
		public float MPPercent => _MP / Agent.MP_Max;
		public float MP => _MP;
		public bool Piloting => _Piloting;

		public override void FixedUpdateNetwork()
		{
			if (!HasStateAuthority) return;

			if (GetInput(out NetworkInputData input))
			{
				if (!PlayerLoaded) return;
				if (Agent == null) return;

				_MP = Mathf.Clamp(_MP + Agent.MP_RegenerationRate * Runner.DeltaTime, 0.0f, Agent.MP_Max);
			}
		}

		public override void Spawned()
		{
			if (LocalPlayer != null)
			{
				Debug.LogError("Multiple local players detected!");
				return;
			}
			LocalPlayer = this;

			if (!PlayerLoaded && HasStateAuthority)
			{
				LiteNetworkManager.QueueNewNetworkObject(LoadingPlayerPrefab, new TransformData(), obj => {
					UnityEngine.Debug.Log("New agent index is " + obj.Index + ".");
					AgentNetworkIndex = (ushort)obj.Index;
				});
			}

			GameManager.Instance.SceneLoader.FinishedLoading();

			if (HasInputAuthority)
			{
				Interactable.OnAnyInteract += OnAnyInteract;
			}
		}

		[SerializeField] private RadialOptionSO[] DefaultInteractionOptions;

		private void OnAnyInteract(Interactable interactable)
		{
			if (Piloting && interactable == null)
			{
				RPC_UnsetPilot(SessionManager.Instance.NetworkRunner.LocalPlayer);
				return;
			}

			if (Agent == null)
			{
				Debug.LogWarning("Player trying to interact when agent not set!");
				return;
			}

			IEnumerable<RadialOptionSO> options = DefaultInteractionOptions.Concat(Agent.InteractionOptions);

			if (interactable?.InteractionOptions != null)
			{
				options = options.Concat(interactable.InteractionOptions);
			}
				
			options = options.Where(o => o.IsValidTarget(interactable));

			RadialMenu.Instance.Show(options.ToArray(), interactable);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_ChangeMP(float change) => _MP += change;

		public static void OnAgentNetworkIndex(Changed<Player> changed)
		{
			changed.Behaviour._agent = null;

			if (changed.Behaviour.HasStateAuthority)
			{
				if (changed.Behaviour.Agent != null)
				{
					MessageBox.Instance.RPC_ShowMessage("Player changed character to " + changed.Behaviour.Agent.gameObject.name + "!");
				}
				else {
					MessageBox.Instance.RPC_ShowMessage("Player is currently loading character!");
				}

				changed.Behaviour._MP = 0;
			}
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_ChangePlayerAgent(byte index)
		{
			UnityEngine.Debug.Log("Changing player agent to " + index + "! Previous agent was " + AgentNetworkIndex + ".");

			TransformData? transformData = null;
			if (AgentNetworkIndex != ushort.MaxValue)
			{
				transformData = LiteNetworkManager.TryGetTransformData(AgentNetworkIndex);
				LiteNetworkManager.QueueDeleteLiteObject(AgentNetworkIndex);
			}

			LiteNetworkManager.QueueNewNetworkObject(
				GameManager.Instance.Characters[index],
				transformData ?? default,
				obj => {
					UnityEngine.Debug.Log("New agent index is " + obj.Index + ".");
					AgentNetworkIndex = (ushort)obj.Index;
					LiteNetworkManager.SetInputAuthority(AgentNetworkIndex, Object.InputAuthority);
				}
			);


			
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_SetPilot(int tower, PlayerRef playerRef)
		{
			if (HasStateAuthority)
				LiteNetworkManager.SetInputAuthority(tower, playerRef);
			
			_Piloting = true;
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_UnsetPilot(int tower)
		{
			if (HasStateAuthority)
				LiteNetworkManager.RemoveInputAuthority(tower);

			_Piloting = false;
		}

		public static void OnPilotChanged(Changed<Player> changed) => changed.Behaviour.SetPilotVisuals();
		private void SetPilotVisuals()
		{
			if (HasStateAuthority)
				Agent.SetPilotState(_Piloting);

			Agent.SetPilotGraphics(_Piloting);
		}
	}
}