using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Pnak
{
	public class Enemy : StateBehaviour
	{
		[SerializeField]
		private DataSelectorWithOverrides<MovementAmount> MovementDataSelector = new DataSelectorWithOverrides<MovementAmount>();

		private Vector3 TargetPosition { get; set; }
		public Vector3 PreviousPosition { get; set; }
		private byte CurrentPathIndex = 0;
		private Transform path;

		private int endHoldTick = -1;
		private int EndHoldTick
		{
			get => endHoldTick;
			set {
				if (value == endHoldTick) return;
				endHoldTick = value;
			}
		}

		public void AddOverride(DataOverride<MovementAmount> dataOverride)
		{
			float previousTime = MovementDataSelector.CurrentData.HoldDuration;
			MovementDataSelector.AddOverride(dataOverride);
			InterpolateHoldTime(previousTime);
		}

		public void RemoveOverride(DataOverride<MovementAmount> dataOverride)
		{
			float previousTime = MovementDataSelector.CurrentData.HoldDuration;
			MovementDataSelector.RemoveOverride(dataOverride);
			InterpolateHoldTime(previousTime);
		}

		public void ModifyOverride(DataOverride<MovementAmount> dataOverride)
		{
			float previousTime = MovementDataSelector.CurrentData.HoldDuration;
			MovementDataSelector.ModifyOverride(dataOverride);
			InterpolateHoldTime(previousTime);
		}

		public void InterpolateHoldTime(float previousTime)
		{
			float startReloadTick = EndHoldTick - (previousTime / Runner.DeltaTime);
			float progress = (Runner.Tick - startReloadTick) / (float)(EndHoldTick - startReloadTick);

			EndHoldTick = Runner.Tick + (int)((MovementDataSelector.CurrentData.HoldDuration * (1f - progress)) / Runner.DeltaTime);
		}

		public void MoveToNext()
		{
			MovementDataSelector.MoveToNext();
			EndHoldTick = Runner.Tick + (int)(MovementDataSelector.CurrentData.HoldDuration / Runner.DeltaTime);
		}


		public void SetNextPosition()
		{
			CurrentPathIndex++;

			if (CurrentPathIndex >= path.childCount)
			{
				LiteNetworkManager.QueueDeleteLiteObject(Controller.NetworkContext);
				return;
			}

			PreviousPosition = TargetPosition;

			var target = Random.Range(0, path.GetChild(CurrentPathIndex).childCount);
			TargetPosition = path.GetChild(CurrentPathIndex).GetChild(target).position;
		}

		public void RevertToPreviousPosition()
		{
			if (CurrentPathIndex <= 1)
				return;

			CurrentPathIndex--;
			TargetPosition = PreviousPosition;

			var target = Random.Range(0, path.GetChild(CurrentPathIndex - 1).childCount);
			PreviousPosition = path.GetChild(CurrentPathIndex - 1).GetChild(target).position;
		}

		public void Init(Transform path)
		{
			this.path = path;

			if (path != null)
			{
				TargetPosition = Controller.TransformCache.Value.Position;
				SetNextPosition();
			}
		}

		public override void InputFixedUpdateNetwork()
		{
			if (path == null)
			{
				UnityEngine.Debug.LogError("Path is null!  This object must be initialized with custom values using the callback.");
				return;
			}

			if (Runner.Tick >= EndHoldTick)
			{
				MoveToNext();
			}

			float movement = MovementDataSelector.CurrentData.MovementSpeed * Runner.DeltaTime;

			TransformData transformData = Controller.TransformCache;

			if (movement >= 0)
			{
				transformData.Position = Vector3.MoveTowards(transformData.Position, TargetPosition, movement);

				if (Vector3.Distance(transformData.Position, TargetPosition) < 0.1f)
					SetNextPosition();
			}
			else
			{
				transformData.Position = Vector3.MoveTowards(transformData.Position, PreviousPosition, -movement);

				if (CurrentPathIndex >= 2 && Vector3.Distance(transformData.Position, PreviousPosition) < 0.1f)
					RevertToPreviousPosition();
			}
	
			Controller.TransformCache.Value = transformData;
		}
	}
}
