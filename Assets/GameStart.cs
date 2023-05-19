using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Pnak
{
	public class GameStart : MonoBehaviour
	{
		[Tooltip("Objects to create on start.")]
		[SerializeField, Sirenix.OdinInspector.AssetsOnly] private GameObject[] _createOnStart;
		[Tooltip("Progress bar to update as objects are created.")]
		[SerializeField] private UIProgressBar _progressBar;
		[Tooltip("Event to invoke when the game has finished loading.")]
		public UnityEvent OnFinishedLoading;

		public SceneAsset SceneAsset;

		private IEnumerator Start()
		{
			lastUpdateTime = Time.timeAsDouble;
			yield return null;

			var totalCount = _createOnStart.Length + 1;
			var currentCount = 1;

			foreach(var obj in _createOnStart)
			{
				Instantiate(obj);

				if (NeedsUpdate())
				{
					_progressBar.Value = (float)(currentCount) / totalCount;
					yield return null;
					currentCount++;
				}
			}

			_progressBar.Value = 1;
			OnFinishedLoading.Invoke();

			GameManager.Instance.AddButtonListener(GameManager.Buttons.MenuButton_1, HostGame);
			GameManager.Instance.AddButtonListener(GameManager.Buttons.MenuButton_2, JoinGame);
		}

		private void OnDestroy()
		{
			GameManager.Instance.RemoveButtonListener(GameManager.Buttons.MenuButton_1, HostGame);
			GameManager.Instance.RemoveButtonListener(GameManager.Buttons.MenuButton_2, JoinGame);
		}

		/// <summary>
		/// The time of the last update.
		/// </summary>
		private double lastUpdateTime = 0;
		/// <summary>
		/// The interval which the game start will break to update the game.
		/// This is to prevent the game from freezing while loading.
		/// </summary>
		private const double updateInterval = 0.01;

		/// <summary>
		/// Returns true if the game start needs to yield for update.
		/// </summary>
		private bool NeedsUpdate()
		{
			var currentTime = Time.timeAsDouble;
			var result = currentTime - lastUpdateTime > updateInterval;
			lastUpdateTime = currentTime;
			return result;
		}

		public void JoinGame()
		{
			SessionManager.Instance.StartGame(1, Fusion.GameMode.Client);
		}

		public void HostGame()
		{
			SessionManager.Instance.StartGame(1, Fusion.GameMode.Host);
		}

		public void QuitGame()
		{
			Application.Quit();
		}
	}
}