using System;
using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pnak
{

	[RequireComponent(typeof(Animator))]
	public class SceneLoader : NetworkSceneManagerDefault
	{
		public const string BeginTransitionState = "BeginTransition";
		public const string BeginTransitionTrigger = "BeginTransition";
		public const string EndTransitionTrigger = "EndTransition";
		public Animator Animator { get; private set; }

		protected void Awake()
		{
			Animator = GetComponent<Animator>();

			DontDestroyOnLoad(gameObject);
		}

		private Action<Scene> _onLoaded;

		public void FinishedLoading()
		{
			Animator.SetTrigger(EndTransitionTrigger);
		}

		public void LoadSceneAsync(int buildIndex, Action<Scene> loaded)
		{
			_onLoaded = loaded;
			Animator.SetTrigger(BeginTransitionTrigger);
			StartCoroutine(LoadSceneAsync(buildIndex, new LoadSceneParameters(LoadSceneMode.Single)));
		}

		protected override YieldInstruction LoadSceneAsync(SceneRef sceneRef, LoadSceneParameters parameters, Action<Scene> loaded)
		{
			_onLoaded = loaded;
			Animator.SetTrigger(BeginTransitionTrigger);
			return StartCoroutine(LoadSceneAsync(sceneRef, parameters));
		}

		private IEnumerator LoadSceneAsync(SceneRef sceneRef, LoadSceneParameters parameters)
		{
			if (!TryGetScenePath(sceneRef, out var scenePath)) {
				throw new InvalidOperationException($"Not going to load {(int)sceneRef}: unable to find the scene name");
			}

			do {
				yield return null;
			} while (Animator.GetCurrentAnimatorStateInfo(0).IsName(BeginTransitionState));

			var acyncOperation = SceneManager.LoadSceneAsync(scenePath, parameters);
			acyncOperation.allowSceneActivation = false;

			while (acyncOperation.progress < 0.9f)
			{
				yield return null;
			}

			acyncOperation.allowSceneActivation = true;

			while (!acyncOperation.isDone)
			{
				yield return null;
			}

			if (_onLoaded != null)
			{
				_onLoaded.Invoke(SceneManager.GetSceneByPath(scenePath));
			}
		}

		protected override YieldInstruction UnloadSceneAsync(Scene scene)
		{
			UnityEngine.Debug.LogError("UnloadSceneAsync is not currently implemented with screen transitions. Make sure that there is intentionally more than one scene loaded at this time.");
			return base.UnloadSceneAsync(scene);
		}
	}

}
