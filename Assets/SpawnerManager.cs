using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace Pnak
{
	public class SpawnerManager : NetworkBehaviour
	{
		[Networked] private TickTimer delay { get; set; }

		[SerializeField] private Enemy _Enemy;
		[SerializeField] private SpawnPattern _SpawnPattern;
		[SerializeField] private Transform _SpawnPath;

		private int _SpawnIndex = 0;

		public override void Spawned()
		{
			base.Spawned();

			if (Runner.IsServer)
			{
				delay = TickTimer.CreateFromSeconds(Runner, _SpawnPattern[_SpawnIndex].delay);
			}
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
							_SpawnIndex = 0;
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

			Runner.Spawn(_Enemy, spawnPos, transform.rotation, Object.InputAuthority, (runner, o) =>
			{
				var enemy = o.GetComponent<Enemy>();
				enemy.Init(_SpawnPath, _SpawnPattern[_SpawnIndex].speed, _SpawnPattern[_SpawnIndex].health * SessionManager.Instance.PlayerCount);
			});
		}
	}
}