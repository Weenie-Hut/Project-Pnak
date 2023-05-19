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

		/// <summary> True if an instance of the manager exists, used to prevent creating a new instance </summary>
		public static bool Exists => _instance != null;

		/// <summary>
		/// The instance of the manager.
		/// If none exists, one will be created in the root of the scene unless the application is quitting.
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
					// If quitting, return null to prevent creating a new instance. All using uses of instances in cases of quitting should be null checked, like when an object is destroyed.
					if (ApplicationWantsToQuit.IsQuitting) return null;

					_instance = FindObjectOfType<T>(); // Finds type if instance hasn't been "Awaked" yet.
					if (_instance == null)
					{
						Debug.LogWarning("No " + typeof(T).Name + " in scene! Creating one...");
						var go = new GameObject(typeof(T).Name);
						_instance = go.AddComponent<T>();
					}
					else {
						Debug.LogWarning(typeof(T).Name + " is found in scene but hasn't been Awaked yet! This may result in uninitialized data and does result in slower scene load performance. Change the hierarchy order or script execution priority to fix.");
					}
				}
				return _instance;
			}
			private set => _instance = value;
		}

		[Tooltip("If true, on awake, the manager will be set to be not destroyed when loading a new scene.")]
		/// <summary> If true, on awake, the manager will be set to be not destroyed when loading a new scene. </summary>
		public bool _DontDestroyOnLoad = false;

		/// <summary>
		/// Initializes the singleton instance if none exists. Otherwise destroys self.
		/// </summary>
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

		/// <summary>
		/// If this is the instance, set the instance to null. Prevents the instance from used/destroyed while being destroyed (like scene load).
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}
	}
}