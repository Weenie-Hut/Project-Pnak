using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Pnak
{
	public class GameStart : MonoBehaviour
	{
		[Tooltip("Objects to create on start.")]
		[SerializeField] private GameObject[] _CreateOnStart;
		[Tooltip("Progress bar to update as objects are created.")]
		[SerializeField] private UIFillBar _ProgressBar;
		[Tooltip("Event to invoke when the game has finished loading.")]
		public UnityEvent OnFinishedLoading;

		private int _progressSteps;
		private int _currentProgressStep;

		private void Awake()
		{
			_progressSteps = _CreateOnStart.Length + 1;
			_currentProgressStep = 0;

			UpdateSteps();
		}

		private void UpdateSteps(int inc = 1)
		{
			_currentProgressStep += inc;
			_ProgressBar.Value = (float)_currentProgressStep / _progressSteps;
		}

		private IEnumerator Start()
		{
			lastUpdateTime = Time.timeAsDouble;
			yield return null; // Wait for the first frame to start loading objects.

			foreach(var obj in _CreateOnStart)
			{
				Instantiate(obj);

				if (NeedsUpdate())
				{
					UpdateSteps();
					yield return null;
				}
			}

			_ProgressBar.Value = 1;
			OnFinishedLoading.Invoke();

			UnityEngine.Debug.Log(GameManager.Instance.InputData.ControllerConfig + " " + GameManager.Instance.PlayerInput.currentActionMap.name);

			GameManager.Instance.AddButtonListener(GameManager.Buttons.MenuButton_1, HostGame);
			GameManager.Instance.AddButtonListener(GameManager.Buttons.MenuButton_2, JoinGame);
		}

		private void OnDestroy()
		{
			GameManager.Instance?.RemoveButtonListener(GameManager.Buttons.MenuButton_1, HostGame);
			GameManager.Instance?.RemoveButtonListener(GameManager.Buttons.MenuButton_2, JoinGame);
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