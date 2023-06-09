using System.Collections.Generic;
using UnityEngine;

namespace Pnak
{
	public abstract class ReferencePool<T> where T : class
	{
		protected T prefab;
		public Stack<T> stack = new Stack<T>();

		public ReferencePool(T prefab)
		{
			this.prefab = prefab;
		}

		public T Get()
		{
			if (stack.Count == 0)
			{
				return CreateInstance();
			}

			return stack.Pop();
		}

		public void Return(T instance)
		{
			stack.Push(instance);
		}

		protected abstract T CreateInstance();
	}

	public class ClassPool<T> : ReferencePool<T> where T : class, new()
	{
		public ClassPool() : base(null) { }

		protected override T CreateInstance()
		{
			return new T();
		}
	}

	public class GameObjectPool : ReferencePool<GameObject>
	{
		public GameObjectPool(GameObject prefab) : base(prefab) { }

		protected override GameObject CreateInstance()
		{
			return GameObject.Instantiate(prefab);
		}
	}

	public class ComponentPool<T> : ReferencePool<T> where T : Component
	{
		public ComponentPool(T prefab) : base(prefab) { }

		protected override T CreateInstance()
		{
			return GameObject.Instantiate(prefab.gameObject).GetComponent<T>();
		}
	}
}