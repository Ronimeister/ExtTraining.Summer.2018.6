﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericCollections
{
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
        public int Count { get; set; }

        public bool IsReadOnly => false;
        #endregion

        #region .ctors
        public Set()
        {
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
            _comparer = EqualityComparer<T>.Default;
        }

        public Set(int capacity)
        {
            if (capacity == 0)
            {
                throw new ArgumentException($"{nameof(capacity)} can't be equal to 0!");
            }

            _buckets = new Bucket[capacity];
            _comparer = EqualityComparer<T>.Default;
        }

        public Set(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException($"{nameof(comparer)} can't be equal to null!");
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
        }

        public Set(int capacity, IEqualityComparer<T> comparer)
        {
            if (capacity == 0)
            {
                throw new ArgumentException($"{nameof(capacity)} can't be equal to 0!");
            }

            _comparer = comparer ?? throw new ArgumentNullException($"{nameof(comparer)} can't be equal to null!");
            _buckets = new Bucket[DEFAULT_ARRAY_SIZE];
        }
        #endregion

        #region Public API
        public bool Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException($"{nameof(item)} can't be equal to null!");
            }

            return InnerAdd(item);
        }
        
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

        public bool Contains(T item)
        {
            if (item == null)
            {
                return false;
            }

            return InnerContains(item);
        }

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

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

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

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException($"{nameof(item)} can't be equal to null!");
            }

            return InnerRemove(item);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

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

        private class Bucket
        {
            public T Value { get; }

            public Bucket Next { get; set; }

            public Bucket(T value)
            {
                Value = value;
            }
        }
    }
}