using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GenericCollections
{
    /// <summary>
    /// Class representing Hash set
    /// </summary>
    /// <typeparam name="T">Type of hash set item value</typeparam>
    public class Set<T> : ISet<T>
    {
        #region Constants and readonly fields
        private const int DEFAULT_ARRAY_SIZE = 3;
        private const int LOWER_31BIT_MASK = 0x7FFFFFFF;

        private readonly IEqualityComparer<T> _comparer;
        #endregion

        #region Private fields
        private Bucket[] _buckets;
        private int _version;
        #endregion

        #region Properties
        public int Count { get; private set; }

        public bool IsReadOnly => false;
        #endregion

        #region .ctors
        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        public Set()
        {
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
            _comparer = EqualityComparer<T>.Default;
        }

        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        /// <param name="capacity">The capacity of the this <see cref="Set{T}"/> instance</param>
        /// <exception cref="ArgumentException">Throws when <paramref name="capacity"/> is equal to null or less than 0</exception>
        public Set(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException($"{nameof(capacity)} can't be equal to 0!");
            }

            _buckets = new Bucket[capacity];
            _comparer = EqualityComparer<T>.Default;
        }

        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        /// <param name="collection">The collection which initialize this <see cref="Set{T}"/> instance</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="collection"/> is equal to null</exception>
        public Set(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException($"{nameof(collection)} can't be equal to null!");
            }

            _comparer = EqualityComparer<T>.Default;
            _buckets = new Bucket[HashHelpers.GetPrime(collection.Count())];

            foreach(var i in collection)
            {
                Add(i);
            }
        }

        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        /// <param name="comparer"><see cref="IEqualityComparer{T}"/> comparer for this <see cref="Set{T}"/> instance</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="comparer"/> is equal to null</exception>
        public Set(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException($"{nameof(comparer)} can't be equal to null!");
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
        }

        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        /// <param name="capacity">The capacity of the this <see cref="Set{T}"/> instance</param>
        /// <param name="comparer"><see cref="IEqualityComparer{T}"/> comparer for this <see cref="Set{T}"/> instance</param>
        /// <exception cref="ArgumentException">Throws when <paramref name="capacity"/> is equal to null or less than 0</exception>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="comparer"/> is equal to null</exception>
        public Set(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException($"{nameof(capacity)} can't be equal to 0!");
            }

            _comparer = comparer ?? throw new ArgumentNullException($"{nameof(comparer)} can't be equal to null!");
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
        }

        /// <summary>
        /// .ctor for <see cref="Set{T}"/> class
        /// </summary>
        /// <param name="collection">The collection which initialize this <see cref="Set{T}"/> instance</param>
        /// <param name="comparer"><see cref="IEqualityComparer{T}"/> comparer for this <see cref="Set{T}"/> instance</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="collection"/> is equal to null</exception>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="comparer"/> is equal to null</exception>
        public Set(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException($"{nameof(collection)} can't be equal to null!");
            }

            _comparer = comparer ?? throw new ArgumentNullException($"{nameof(comparer)} can't be equal to null!");
            _buckets = new Bucket[HashHelpers.GetPrime(collection.Count())];

            foreach (var i in collection)
            {
                Add(i);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Adds an element to the current set and returns a value to indicate if the element was successfully added.
        /// </summary>
        /// <param name="item">Added element</param>
        /// <returns>A value to indicate if the element was successfully added</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="item"/> is equal to null</exception>
        public bool Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException($"{nameof(item)} can't be equal to null!");
            }

            return InnerAdd(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="Set{T}"/>
        /// </summary>
        public void Clear()
        {
            if (Count == 0)
            {
                return;
            }

            Array.Clear(_buckets, 0, _buckets.Length);
            Count = 0;
            _version++;
        }

        /// <summary>
        /// Determines whether the <see cref="Set{T}"/> contains a specific value
        /// </summary>
        /// <param name="item">A specific value to find</param>
        /// <returns>Finding result</returns>
        public bool Contains(T item)
        {
            if (item == null)
            {
                return false;
            }

            return InnerContains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="Set{T}"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index (<paramref name="arrayIndex"/>)
        /// </summary>
        /// <param name="array">Needed <see cref="Array"/></param>
        /// <param name="arrayIndex">A particular <see cref="Array"/> index</param>
        /// <exception cref="ArgumentException">Throws when <paramref name="array"/> is smaller than this <see cref="Set{T}"/></exception>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="array"/> is equal to null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws when:
        /// 1) <paramref name="arrayIndex"/> is less than 0
        /// 2) <paramref name="arrayIndex"/> is bigger or equal than <paramref name="array"/> length
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException($"{nameof(array)} can't be equal to null!");
            }

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException($"{nameof(arrayIndex)} is out of range!");
            }

            if (Count > array.Length - arrayIndex + 1)
            {
                throw new ArgumentException($"{nameof(array)} should be bigger than collection!");
            }

            foreach(T e in this)
            {
                array[arrayIndex] = e;
                arrayIndex++;
            }
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current set
        /// </summary>
        /// <param name="other">The specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (other == this)
            {
                Clear();
                return;
            }

            foreach(T e in other)
            {
                Remove(e);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Set{T}"/>
        /// </summary>
        /// <returns>An enumerator that iterates through the <see cref="Set{T}"/></returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach(Bucket b in _buckets)
            {
                Bucket current = b;
                while (current != null)
                {
                    yield return current.Value;
                    current = current.Next;
                }
            }
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are also in a specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null or any element of this collection is equal to null</exception>
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            Set<T> prevSet = new Set<T>(this);
            Set<T> newSet = new Set<T>(other);

            Clear();

            foreach(T e in newSet)
            {
                if (prevSet.Contains(e))
                {
                    Add(e);
                }
            }
        }

        /// <summary>
        /// Determines whether the current set is a proper (strict) subset of a specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>New collection</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (ReferenceEquals(this, other))
            {
                return false;
            }

            Set<T> otherSet = new Set<T>(other);

            if (Count > otherSet.Count)
            {
                return false;
            }

            return this.All(otherSet.Contains);
        }

        /// <summary>
        /// Determines whether the current set is a proper (strict) superset of a specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>New collection</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            return new Set<T>(other).IsProperSubsetOf(this);
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>New collection</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Count == 0)
            {
                return true;
            }

            Set<T> otherSet = new Set<T>(other);

            if (Count > otherSet.Count)
            {
                return false;
            }

            return this.All(otherSet.Contains);
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>New collection</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            Set<T> otherSet = new Set<T>(other);

            return otherSet.IsSubsetOf(this);
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection
        /// </summary>
        /// <param name="other">A specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>The result of overlaping</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (Count == 0)
            {
                return false;
            }

            foreach(T e in other)
            {
                if (Contains(e))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="Set{T}"/>
        /// </summary>
        /// <param name="item">A specific object</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="item"/> is equal to null</exception>
        /// <returns>Bool result of removing success</returns>
        public bool Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException($"{nameof(item)} can't be equal to null!");
            }

            return InnerRemove(item);
        }

        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements
        /// </summary>
        /// <param name="other">The specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        /// <returns>Bool result of equality</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            Set<T> otherSet = new Set<T>(other.ToList());

            if (otherSet.Count != Count)
            {
                return false;
            }

            return otherSet.All(Contains);
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both
        /// </summary>
        /// <param name="other">The specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            if (Count == 0)
            {
                UnionWith(other);
            }

            if (other == this)
            {
                Clear();
                return;
            }

            foreach (T e in other)
            {
                if (Contains(e))
                {
                    Remove(e);
                }
                else
                {
                    Add(e);
                }
            }
        }

        /// <summary>
        /// Modifies the <paramref name="lhs"/> set so that it contains all elements that are present in the current set, in the specified collection, or in both
        /// </summary>
        /// <param name="lhs">First collection</param>
        /// <param name="rhs"></param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="lhs"/> or <paramref name="rhs"/> is equal to null</exception>
        /// <returns></returns>
        public static ISet<T> Union(ISet<T> lhs, ISet<T> rhs)
        {
            if (lhs == null)
            {
                throw new ArgumentNullException($"{nameof(lhs)} can't be equal to null!");
            }

            if (rhs == null)
            {
                throw new ArgumentNullException($"{nameof(rhs)} can't be equal to null!");
            }

            lhs.UnionWith(rhs);

            return lhs;
        }

        /// <summary>
        /// Modifies the current set so that it contains all elements that are present in the current set, in the specified collection, or in both
        /// </summary>
        /// <param name="other">The specified collection</param>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="other"/> is equal to null</exception>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException($"{nameof(other)} can't be equal to null!");
            }

            foreach(T e in other)
            {
                Add(e);
            }
        }

        void ICollection<T>.Add(T item) => Add(item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Private methods
        private int GetIndex(T value)
            => Math.Abs(InternalGetHashCode(value) % _buckets.Length);

        private bool InnerAdd(T item)
        {
            int index = GetIndex(item);

            if (_buckets[index] == null)
            {
                _buckets[index] = new Bucket(item);
                Count++;

                if (IsBucketsFull())
                {
                    Resize();
                }
            }
            else
            {
                if (!Contains(item))
                {
                    Bucket parent = _buckets[index];

                    parent.Next = new Bucket(item);
                    Count++;
                }
            }

            _version++;

            return true;
        }

        private bool InnerContains(T item)
        {
            int index = GetIndex(item);

            Bucket current = _buckets[index];

            while (current != null)
            {
                if (_comparer.Equals(current.Value, item))
                {
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        private bool InnerRemove(T item)
        {
            int index = GetIndex(item);
            Bucket current = _buckets[index];
            Bucket parent = _buckets[index];

            if (current == null)
            {
                return false;
            }

            if (_comparer.Equals(current.Value, item))
            {
                if (current.Next == null)
                {
                    _buckets[index] = null;
                }
                else
                {
                    _buckets[index] = current.Next;
                }

                Count--;
                _version++;

                return true;
            }

            while (current != null)
            {
                if (_comparer.Equals(current.Value, item))
                {
                    parent.Next = current.Next;
                    Count--;

                    return true;
                }

                SetNextBucket(ref parent, ref current);
            }

            return false;
        }

        private int InternalGetHashCode(T value)
        {
            if (value == null)
            {
                return 0;
            }

            return _comparer.GetHashCode(value) & LOWER_31BIT_MASK;
        }

        private bool IsBucketsFull() => Count == _buckets.Length;

        private void Resize()
        {
            int newCapacity = HashHelpers.IncreaseCapacity(_buckets.Length);
            Bucket[] oldBuckets = _buckets;

            _buckets = new Bucket[newCapacity];
            Count = 0;

            foreach(Bucket b in oldBuckets)
            {
                Bucket current = b;

                while(current != null)
                {
                    Add(current.Value);
                    current = current.Next;
                }
            }

            _version++;
        }

        private void SetNextBucket(ref Bucket parent, ref Bucket current)
        {
            parent = current;
            current = current.Next;
        }
        #endregion

        /// <summary>
        /// Helper class representing <see cref="Set{T}"/> bucket
        /// </summary>
        private class Bucket
        {
            /// <summary>
            /// Value of the item
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// Next item in bucket
            /// </summary>
            public Bucket Next { get; set; }

            /// <summary>
            /// .ctor for <see cref="Bucket"/>
            /// </summary>
            /// <param name="value">Value of the item</param>
            public Bucket(T value)
            {
                Value = value;
            }
        }
    }
}
