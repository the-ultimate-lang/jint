namespace Ultimate.Language.Jint.Runtime
{
    internal sealed class OrderedSet<T>
    {
        internal readonly List<T> _list;
        private readonly HashSet<T> _set;

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _list = new List<T>();
            _set = new HashSet<T>(comparer);
        }

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (_set.Add(value))
                {
                    _list[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            if (_set.Add(item))
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            _list.Clear();
            _set.Clear();
        }

        public bool Contains(T item) => _set.Contains(item);

        public int Count => _list.Count;

        public bool Remove(T item)
        {
            _set.Remove(item);
            return _list.Remove(item);
        }
    }
}
