using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace OnlineGame.Utility.Types
{
    /// <summary>
    /// A thread-safe list implementation that allows concurrent reads and synchronized writes.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class ThreadSafeList<T>
    {
        private readonly List<T> _internalList = [];
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Adds an item to the list in a thread-safe manner.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _internalList.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds a range of items to the list in a thread-safe manner.
        /// </summary>
        /// <param name="items">The collection of items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            _lock.EnterWriteLock();
            try
            {
                _internalList.AddRange(items);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes an item from the list in a thread-safe manner.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if the item was removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _internalList.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets an item at a specific index in a thread-safe manner.
        /// </summary>
        /// <param name="index">The zero-based index of the item.</param>
        /// <returns>The item at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _internalList[index];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    _internalList[index] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets a snapshot of the current items in the list in a thread-safe manner.
        /// </summary>
        /// <returns>A copy of the items in the list.</returns>
        public List<T> ToList()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<T>(_internalList);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Clears all items from the list in a thread-safe manner.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _internalList.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the number of items in the list in a thread-safe manner.
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _internalList.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Checks whether the list contains a specific item in a thread-safe manner.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item is in the list; otherwise, false.</returns>
        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _internalList.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
