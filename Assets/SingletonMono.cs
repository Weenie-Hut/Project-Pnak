using UnityEngine;

namespace Pnak
{
	/// <summary>
	/// A base class for a singleton MonoBehaviour. This class will contains the logic for assigning and maintaining the singleton instance, including:
	/// 1. Creating one if none exists.
	/// 2. Destroying any duplicates.
	/// 3. (optional) Setting the instance to not be destroyed when loading a new scene.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
	{
		/// <summary> Encapsulated instance of the manager. </summary>
		private static T _instance;

		public static bool Exists => _instance != null;

		/// <summary>
		/// The instance of the manager.
		/// If none exists, one will be created in the root of the scene.
		/// </summary>
		public static T Instance
		{
			get {
	#if UNITY_EDITOR
				// If not playing, return the first instance found in the scene (as the instance will not have been set)
				if (Application.isPlaying == false)
				{
					_instance = FindObjectOfType<T>();
					return _instance;
				}
	#endif
				if (_instance == null)
				{
					_instance = FindObjectOfType<T>();
					if (_instance == null)
					{
						Debug.LogWarning("No " + typeof(T).Name + " in scene! Creating one...");
						var go = new GameObject(typeof(T).Name);
						_instance = go.AddComponent<T>();
					}
				}
				return _instance;
			}
			private set => _instance = value;
		}

		[Tooltip("If true, on awake, the manager will be set to be not destroyed when loading a new scene.")]
		/// <summary> If true, on awake, the manager will be set to be not destroyed when loading a new scene. </summary>
		public bool _DontDestroyOnLoad = false;

		protected virtual void Awake()
		{
			if (this is not T)
			{
				Debug.LogWarning("Manager is not of type " + typeof(T).Name + "! Destroying self...");
				Destroy(gameObject);
				return;
			}

			if (_instance == null)
			{
				Instance = this as T;
				if (_DontDestroyOnLoad) DontDestroyOnLoad(gameObject);
			}
			else
			{
				Debug.LogWarning("Multiple " + typeof(T).Name + " in scene! This: " + gameObject.name + ", Instance: " + Instance.gameObject.name + ". Destroying self...  ");
				Destroy(gameObject);
			}
		}
	}
}