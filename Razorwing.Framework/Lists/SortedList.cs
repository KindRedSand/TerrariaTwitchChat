﻿using TwitchChat.Razorwing.Framework.Extensions.TypeExtensions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TwitchChat.Razorwing.Framework.Lists
{
    public class SortedList<T> : ICollection<T>, IReadOnlyList<T>
    {
        private readonly List<T> list;

        public IComparer<T> Comparer { get; }

        public int Count => list.Count;

        bool ICollection<T>.IsReadOnly => ((ICollection<T>)list).IsReadOnly;

        public T this[int index]
        {
            get { return list[index]; }
            set { list[index] = value; }
        }

        public SortedList(Func<T, T, int> comparer) : this(new ComparisonComparer<T>(comparer))
        {
        }

        public SortedList(IComparer<T> comparer)
        {
            list = new List<T>();
            Comparer = comparer;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var i in collection)
                Add(i);
        }

        public virtual void RemoveRange(int index, int count) => list.RemoveRange(index, count);

        public virtual int Add(T value) => addInternal(value);

        /// <summary>
        /// Adds the specified item internally without the interference of a possible derived class.
        /// </summary>
        /// <param name="value">The item to add.</param>
        /// <returns>The index of the item within this list.</returns>
        private int addInternal(T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int index = list.BinarySearch(value, Comparer);
            if (index < 0)
                index = ~index;

            list.Insert(index, value);

            return index;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public virtual void RemoveAt(int index) => list.RemoveAt(index);

        public int RemoveAll(Predicate<T> match)
        {
            List<T> found = (List<T>)FindAll(match);

            foreach (var i in found)
                Remove(i);

            return found.Count;
        }

        public virtual void Clear() => list.Clear();

        public bool Contains(T item) => IndexOf(item) >= 0;

        public int BinarySearch(T value) => list.BinarySearch(value, Comparer);

        public int IndexOf(T value)
        {
            int index = list.BinarySearch(value, Comparer);
            return index >= 0 && list[index].Equals(value) ? index : -1;
        }

        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public T Find(Predicate<T> match) => list.Find(match);

        public IEnumerable<T> FindAll(Predicate<T> match) => list.FindAll(match);

        public T FindLast(Predicate<T> match) => list.FindLast(match);

        public int FindIndex(Predicate<T> match) => list.FindIndex(match);

        public override string ToString() => $@"{GetType().ReadableName()} ({Count} items)";

        #region ICollection<T> Implementation

        void ICollection<T>.Add(T item) => Add(item);

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        #endregion

        private class ComparisonComparer<TComparison> : IComparer<TComparison>
        {
            private readonly Comparison<TComparison> comparison;

            public ComparisonComparer(Func<TComparison, TComparison, int> compare)
            {
                if (compare == null)
                {
                    throw new ArgumentNullException(nameof(compare));
                }
                comparison = new Comparison<TComparison>(compare);
            }

            public int Compare(TComparison x, TComparison y)
            {
                return comparison(x, y);
            }
        }
    }
}
