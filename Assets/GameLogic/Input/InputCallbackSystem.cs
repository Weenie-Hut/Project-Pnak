using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pnak.Input
{
	public static class InputCallbackSystem
	{
		public class RegisterData
		{
			public readonly string actionName;
			public readonly Predicate<InputAction.CallbackContext> filter;
			public readonly InputContextAction action;

			public RegisterData(string actionName, Predicate<InputAction.CallbackContext> filter, InputContextAction action)
			{
				this.actionName = actionName;
				this.filter = filter;
				this.action = action;
			}
		}

		public static void OnActionTriggered(InputAction.CallbackContext context)
		{
			if (_actionDictionary.TryGetValue(context.action.name, out List<RegisterData> actions))
			{
				foreach (var entry in actions)
				{
					if (entry.filter == null || entry.filter(context))
						entry.action(context);
				}
			}
		}

		private static Dictionary<string, List<RegisterData>> _actionDictionary = new Dictionary<string, List<RegisterData>>();

		[System.NonSerialized] private static Dictionary<Type, Func<object, RegisterData>[]> _classActionFactory = new Dictionary<Type, Func<object, RegisterData>[]>();
		[System.NonSerialized] private static Dictionary<object, RegisterData[]> _classActionCache = new Dictionary<object, RegisterData[]>();

		/// <summary>
		/// Creates the input actions for the given object.
		/// </summary>
		public static void CreateInputCallbacks(object obj, bool cacheFactory = true)
		{
			_CreateObjectInputActions(obj, cacheFactory);
		}

		/// <summary>
		/// Deletes the input actions for the given object.
		/// </summary>
		public static void DeleteInputCallbacks(object obj)
		{
			_DestroyAbjectInputActions(obj, out RegisterData[] _);
		}

		/// <summary>
		/// Creates and registers the input actions for the given object.
		/// </summary>
		public static void SetupInputCallbacks(object obj, bool cacheFactory = true)
		{
			RegisterData[] actions = _CreateObjectInputActions(obj, cacheFactory);
			_RegisterCallbacks(obj, actions);
		}

		/// <summary>
		/// Deletes and unregisters the input actions for the given object.
		/// </summary>
		public static void CleanupInputCallbacks(object obj)
		{
			if (_DestroyAbjectInputActions(obj, out RegisterData[] actions))
				_UnregisterCallbacks(obj, actions);
		}

		/// <summary>
		/// Registers the input actions for the given object.
		/// </summary>
		public static void RegisterInputCallbacks(object obj)
		{
			RegisterData[] actions = _GetRegisterData(obj);
			_RegisterCallbacks(obj, actions);
		}

		/// <summary>
		/// Unregisters the input actions for the given object.
		/// </summary>
		public static void UnregisterInputCallbacks(object obj)
		{
			RegisterData[] actions = _GetRegisterData(obj);
			_UnregisterCallbacks(obj, actions);
		}

		private static RegisterData[] _GetRegisterData(object obj)
		{
			RegisterData[] actions;
			if (!_classActionCache.TryGetValue(obj, out actions))
			{
				UnityEngine.Debug.LogError($"No cached actions for {obj.GetType().Name} found. Check to make sure you are only calling 'CreateInputCallbacks' or 'SetupInputCallbacks' (either, only once) for each object before registering or unregistering/deleting.");
				actions = _CreateObjectInputActions(obj);
			}
			return actions;
		}

		private static void _RegisterCallbacks(object obj, RegisterData[] actions)
		{
			foreach (var entry in actions)
			{
				if (!_actionDictionary.ContainsKey(entry.actionName))
					_actionDictionary.Add(entry.actionName, new List<RegisterData>());

				_actionDictionary[entry.actionName].Add(entry);
			}
		}
		
		private static void _UnregisterCallbacks(object obj, RegisterData[] actions)
		{
			foreach (var entry in actions)
			{
				if (!_actionDictionary.ContainsKey(entry.actionName))
				{
					UnityEngine.Debug.LogError($"No action dictionary entry for {entry.actionName} found. Skipping.");
					continue;
				}

				if (!_actionDictionary[entry.actionName].Remove(entry))
				{
					UnityEngine.Debug.LogWarning($"Action {entry.actionName} was not registered for {obj.GetType().Name}.");

					// For debugging purposes:
					// Log all entries in "_actionDictionary" by action name, the RegisterData hash code.
					string s = $"Action {entry.actionName} was not registered for {obj.GetType().Name} (Reg Hash: {entry.GetHashCode()}).\n";
					foreach (KeyValuePair<string, List<RegisterData>> kvp in _actionDictionary)
					{
						s += $"Action {kvp.Key} has {kvp.Value.Count} entries:\n";
						foreach (RegisterData rd in kvp.Value)
						{
							s += $"  {rd.GetHashCode()}\n";
						}
					}

					UnityEngine.Debug.LogWarning(s);
				}
			}
		}

		private static RegisterData[] _CreateObjectInputActions(object obj, bool cacheFactory = true)
		{
#if DEBUG
			// Validate that the object does not already have cached actions.
			if (_classActionCache.ContainsKey(obj))
			{
				UnityEngine.Debug.LogWarning($"Cached actions for {obj.GetType().Name} found but should not have existed. Check to make sure you are only calling 'CreateInputCallbacks' or 'SetupInputCallbacks' a single time per object. This will likely cause callbacks to be impossible to unregister.");
				_classActionCache.Remove(obj);
			}
#endif
			var type = obj.GetType();

			Func<object, RegisterData>[] factory;

			if (cacheFactory)
			{
				if (!_classActionFactory.TryGetValue(type, out factory))
				{
					factory = CreateClassInputFactory(type);
				}
			}
			else
			{
#if DEBUG
				if (_classActionFactory.TryGetValue(type, out factory))
					UnityEngine.Debug.LogWarning($"Cached factory for {type.Name} found but object was created without using cache. The CacheFactory flag is intended for Singletons and other objects that are created once and never destroyed.");
				else
#endif
					factory = CreateClassInputFactory(type);
			}

			RegisterData[] result = new RegisterData[factory.Length];
			for (int i = 0; i < factory.Length; i++)
			{
				result[i] = factory[i].Invoke(obj);
			}

			_classActionCache.Add(obj, result);

			return result;
		}

		public static bool _DestroyAbjectInputActions(object obj, out RegisterData[] actions)
		{
			if (!_classActionCache.Remove(obj, out actions))
			{
				UnityEngine.Debug.LogWarning($"No cached actions for {obj.GetType().Name} found but attempting to unregister. You either never registered the actions or you already deleted them.");
				return false;
			}
			return true;
		}

		public static Func<object, RegisterData>[] CreateClassInputFactory(Type type)
		{
			Func<object, RegisterData>[] result =
				type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SelectMany(m =>
					m.GetCustomAttributes(typeof(InputActionTriggered), true)
					.Select(a => { 
						InputActionTriggered iat = (InputActionTriggered)a;
						return new Func<object, RegisterData>(obj =>
							new RegisterData(
								iat.actionName,
								iat.GetFilteredCallback(),
								(InputContextAction)Delegate.CreateDelegate(typeof(InputContextAction), obj, m.Name)
							)
						);
					}))
				.ToArray();

			if (result.Length == 0)
			{
				Debug.LogWarning($"No Input Action Callback attributes found on class {type.Name}. Should not be trying to register this class for input callbacks.");
			}

			_classActionFactory.Add(type, result);

			return result;
		}
	}
}