namespace Pnak
{
	public abstract class RCache<T>
	{
		public bool HasValue { get; private set; } = false;
		private bool _enabled = true;

		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled != value)
				{
					Invalidate();
					_enabled = value;
				}
			}
		}

		private T _value;
		public T Value
		{
			get {
				if (!HasValue || !Enabled)
					_value = Fetch();
				return _value;
			}
		}

		public void Invalidate()
		{
			HasValue = false;
		}

		public T Fetch()
		{
			_value = Get();
			HasValue = true;
			return _value;
		}

		public void Force(T value)
		{
			_value = value;
			HasValue = true;
		}

		protected abstract T Get();
		public static implicit operator T(RCache<T> cache) => cache.Value;
	}

	public abstract class Cache<T>
	{
		public bool Dirty { get; private set; } = true;
		public bool HasValue { get; private set; } = false;
		private bool _enabled = true;

		public bool Enabled
		{
			get => _enabled;
			set
			{
				if (_enabled != value)
				{
					Invalidate();
					_enabled = value;
				}
			}
		}

		private T _value;
		public T Value
		{
			get {
				if (!HasValue || !Enabled)
					_value = Fetch();
				return _value;
			}
			set {
				if (!Enabled)
				{
					Set(ref value);
					return;
				}

				_value = value;
				HasValue = true;
				Dirty = true;
			}
		}

		public void Invalidate()
		{
			HasValue = false;
			Dirty = false;
		}

		public T Fetch()
		{
			_value = Get();
			HasValue = true;
			Dirty = false;
			return _value;
		}

		public void Apply()
		{
			if (!HasValue) return;

			if (Dirty)
			{
				Set(ref _value);
				Dirty = false;
			}
		}

		protected abstract T Get();
		protected abstract void Set(ref T value);

		public static implicit operator T(Cache<T> cache) => cache.Value;
	}

	public class RCacheCallbacks<T> : RCache<T>
	{
		public delegate T GetterDelegate();
		public GetterDelegate Getter { get; private set; }
		public RCacheCallbacks(GetterDelegate getter) => Getter = getter;
		protected override T Get() => Getter();
	}

	public class CacheCallbacks<T> : Cache<T>
	{
		public delegate T GetterDelegate();
		public delegate void SetterDelegate(ref T value);

		public GetterDelegate Getter { get; private set; }
		public SetterDelegate Setter { get; private set; }

		public CacheCallbacks(GetterDelegate getter, SetterDelegate setter)
		{
			Getter = getter;
			Setter = setter;
		}

		protected override T Get() => Getter();
		protected override void Set(ref T value) => Setter(ref value);
	}
}