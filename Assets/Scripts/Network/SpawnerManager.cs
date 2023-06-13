using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace Pnak
{
	public class SpawnerManager : NetworkBehaviour
	{
		private static SpawnerManager self;
		public static float GlobalMoney { get { return self.globalMoney; } }

		[Networked] private TickTimer delay { get; set; }
		[Networked(OnChanged = nameof(OnGlobalMoneyChange))] private float globalMoney { get; set; }

		[SerializeField] private SpawnPattern _SpawnPattern;
		[SerializeField] private Transform _SpawnPath;
		[SerializeField] private TMPro.TextMeshProUGUI _MoneyText;
		[SerializeField] private float _StartingMoney = 100.0f;
		private int _SpawnIndex = 0;

		private int loopCount = 0;

		private void Awake()
		{
			self = this;
		}

		public override void Spawned()
		{
			base.Spawned();

			if (Runner.IsServer)
			{
				delay = TickTimer.CreateFromSeconds(Runner, _SpawnPattern[_SpawnIndex].delay);
			}

			loopCount = 0;
			globalMoney = _StartingMoney;
		}

		public override void FixedUpdateNetwork()
		{
			if (!Runner.IsServer) return;

			if (delay.ExpiredOrNotRunning(Runner))
			{
				if (_SpawnIndex >= _SpawnPattern.Length && !_SpawnPattern.Loop) return;

				while (true)
				{
					Spawn();
					
					_SpawnIndex++;
					if (_SpawnIndex >= _SpawnPattern.Length)
					{
						if (_SpawnPattern.Loop)
						{
							loopCount++;
							_SpawnIndex = 0;
						}
						else
							return;
					}

					if (_SpawnPattern[_SpawnIndex].delay > float.Epsilon)
					{
						delay = TickTimer.CreateFromSeconds(Runner, _SpawnPattern[_SpawnIndex].delay);
						break;
					}

					
				}
			}
		}

		private void Spawn()
		{
			int random = Random.Range(0, _SpawnPath.GetChild(0).childCount);
			Vector3 spawnPos = _SpawnPath.GetChild(0).GetChild(random).position;

			TransformData data = new TransformData {
				Position = spawnPos,
			};

			LiteNetworkManager.QueueNewNetworkObject(_SpawnPattern[_SpawnIndex].enemy, data, (enemy) => {
				enemy.Target.GetStateBehaviour<Enemy>().Init(_SpawnPath);
				enemy.Target.GetStateBehaviour<HealthBehaviour>().ScaleHealth((loopCount + 1) * _SpawnPattern.HealthScalePerLoop);
			});
		}

		private static void OnGlobalMoneyChange(Changed<SpawnerManager> changed) => changed.Behaviour._MoneyText.text = $"${changed.Behaviour.globalMoney.ToString("0.00")}";
		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_ChangeMoney_(float amount)
		{
			globalMoney += amount;
		}

		public static void RPC_ChangeMoney(float amount)
		{
			self.RPC_ChangeMoney_(amount);
		}
	}
}