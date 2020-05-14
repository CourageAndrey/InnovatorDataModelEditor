using System.Collections.Generic;
using System.Threading;

namespace IDME.WpfEditor.Helpers
{
	public delegate void ItemDelegate<T>(EventCollection<T> collection, T item);

	public class EventCollection<T> : ICollection<T>
	{
		#region Implementation of ICollection

		public IEnumerator<T> GetEnumerator()
		{
			return _innerList.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			_innerList.Add(item);
			raiseAdded(item);
		}

		public void Clear()
		{
			var clone = new List<T>(_innerList);
			_innerList.Clear();
			foreach (var item in clone)
			{
				raiseRemoved(item);
			}
		}

		public bool Contains(T item)
		{
			return _innerList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_innerList.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			bool result = _innerList.Remove(item);
			raiseRemoved(item);
			return result;
		}

		public int Count
		{ get { return _innerList.Count; } }

		public bool IsReadOnly
		{ get { return false; } }

		#endregion

		#region Properties

		private readonly List<T> _innerList = new List<T>();

		public event ItemDelegate<T> ItemAdded;

		public event ItemDelegate<T> ItemRemoved;

		private void raiseAdded(T item)
		{
			var handler = Volatile.Read(ref ItemAdded);
			if (handler != null)
			{
				handler(this, item);
			}
		}

		private void raiseRemoved(T item)
		{
			var handler = Volatile.Read(ref ItemRemoved);
			if (handler != null)
			{
				handler(this, item);
			}
		}

		#endregion
	}
}
