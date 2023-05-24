using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

using InputContextAction = System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>;
using InputCallbackPair = System.Collections.Generic.KeyValuePair<string, System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>>;

namespace Pnak.Input
{
	public static class InputCallbackSystem
	{
		public static void OnActionTriggered(InputAction.CallbackContext context)
		{
			if (_actionDictionary.TryGetValue(context.action.name, out Action<InputAction.CallbackContext> action))
			{
				action.Invoke(context);
			}
		}

		private static Dictionary<string, InputContextAction> _actionDictionary = new Dictionary<string, InputContextAction>();

		[System.NonSerialized] private static Dictionary<Type, IEnumerable<Func<object, InputCallbackPair>>> _classActionFactory = new Dictionary<Type, IEnumerable<Func<object, InputCallbackPair>>>();
		[System.NonSerialized] private static Dictionary<Type, IEnumerable<InputCallbackPair>> _classActionCache = new Dictionary<Type, IEnumerable<InputCallbackPair>>();
		public static void RegisterInputCallbacks(object obj, bool cacheFactory = true)
		{
			var type = obj.GetType();

			IEnumerable<InputCallbackPair> actions;
			if (!_classActionCache.TryGetValue(type, out actions))
			{
				actions = CreateObjectInputActions(obj, cacheFactory);
				_classActionCache.Add(type, actions);
			}

			foreach (var entry in actions)
			{
				if (!_actionDictionary.ContainsKey(entry.Key))
					_actionDictionary.Add(entry.Key, entry.Value);
				else
					_actionDictionary[entry.Key] += entry.Value;
			}
		}

		public static void UnregisterInputCallbacks(object obj, bool cacheFactory = true)
		{
			UnityEngine.Debug.Log($"Unregistering input callbacks for {obj.GetType().Name}.");

			var type = obj.GetType();

			IEnumerable<InputCallbackPair> actions;
			if (!_classActionCache.TryGetValue(type, out actions))
			{
				UnityEngine.Debug.Log($"No cached actions for {type.Name} found. Creating new cache.");
				actions = CreateObjectInputActions(obj, cacheFactory);
				_classActionCache.Add(type, actions);
			}

			foreach (var entry in actions)
			{
				if (_actionDictionary.ContainsKey(entry.Key))
					_actionDictionary[entry.Key] -= entry.Value;
			}
		}

		private static IEnumerable<InputCallbackPair> CreateObjectInputActions(object obj, bool cacheFactory = true)
		{
			var type = obj.GetType();

			IEnumerable<Func<object, InputCallbackPair>> factory;
			if (!_classActionFactory.TryGetValue(type, out factory))
			{
				factory = GetClassInputFactory(type);

				if (cacheFactory) _classActionFactory.Add(type, factory);
			}

			return factory.Select(f => f(obj));
		}

		public static IEnumerable<Func<object, InputCallbackPair>> GetClassInputFactory(Type type)
		{
			IEnumerable<Func<object, InputCallbackPair>> result =
				type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.SelectMany(m =>
					m.GetCustomAttributes(typeof(InputActionTriggered), true)
					.Select(a => { 
						InputActionTriggered iat = (InputActionTriggered)a;
						return new Func<object, InputCallbackPair>(obj =>
							iat.CreateInputPair(m, obj)
						);
					})
				);

#if UNITY_EDITOR
			if (result.Count() == 0)
			{
				Debug.LogWarning($"No Input Action Callback attributes found on class {type.Name}. Should not be trying to register this class for input callbacks.");
			}
#endif
			return result;
		}
	}
}