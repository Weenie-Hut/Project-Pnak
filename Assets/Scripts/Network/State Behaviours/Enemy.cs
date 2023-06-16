using System.Collections.Generic;
using Fusion;
using UnityEngine;
using System.Linq;

namespace Pnak
{
	public class Enemy : StateBehaviour
	{
		public static List<Enemy> Enemies = new List<Enemy>();
		public static float FurthestEnemyDistance()
		{
			if (Enemies.Count == 0) return 0;
			return Enemies.Max(e => e.DistanceLeft);
		}

		public static float NormalizedFurthestEnemyDistance()
		{
			if (Enemies.Count == 0) return 0;
			return Enemies.Max(e => e.DistanceLeft / e.totalDistance);
		}

		[SerializeField]
		private DataSelectorWithOverrides<MovementAmount> MovementDataSelector = new DataSelectorWithOverrides<MovementAmount>();

		private Vector3 TargetPosition { get; set; }
		public Vector3 PreviousPosition { get; set; }
		private byte CurrentPathIndex = 0;
		private Transform path;

		public int[] pathIndexes;
		private float distanceBase;
		private float totalDistance;

		public float DistanceLeft => distanceBase + Vector3.Distance(TargetPosition, transform.position);

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
				Controller.QueueForDestroy();
				return;
			}

			PreviousPosition = TargetPosition;
			TargetPosition = path.GetChild(CurrentPathIndex).GetChild(pathIndexes[CurrentPathIndex]).position;

			distanceBase -= Vector3.Distance(PreviousPosition, TargetPosition);
		}

		public void RevertToPreviousPosition()
		{
			if (CurrentPathIndex <= 1)
				return;

			CurrentPathIndex--;

			distanceBase += Vector3.Distance(PreviousPosition, TargetPosition);

			TargetPosition = PreviousPosition;
			PreviousPosition = path.GetChild(CurrentPathIndex - 1).GetChild(pathIndexes[CurrentPathIndex - 1]).position;
		}

		public void Init(Transform path)
		{
			this.path = path;

			if (path != null)
			{
				pathIndexes = new int[path.childCount];
				distanceBase = 0f;
				for (int i = 0; i < path.childCount; i++)
				{
					pathIndexes[i] = Random.Range(0, path.GetChild(i).childCount);
					if (i > 0)
						distanceBase += Vector3.Distance(
							path.GetChild(i - 1).GetChild(pathIndexes[i - 1]).position,
							path.GetChild(i).GetChild(pathIndexes[i]).position);
				}
				totalDistance = distanceBase;

				TargetPosition = Controller.TransformCache.Value.Position;
				SetNextPosition();
			}

			Enemies.Add(this);
		}

		public override void QueuedForDestroy()
		{
			base.QueuedForDestroy();

			Enemies.Remove(this);
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
