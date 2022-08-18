﻿using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic
{
    public ref partial struct TempArray<T>
    {
        internal static readonly bool s_clearArray = SystemRuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private static readonly T[] s_emptyArray = new T[0];

        internal T[] _array; // Do not rename (binary serialization)
        internal int _length; // Do not rename (binary serialization)

        [NonSerialized]
        internal ArrayPool<T> _pool;

        internal TempArray(int length, ArrayPool<T> pool)
        {
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            _length = length;
            _pool = pool ?? ArrayPool<T>.Shared;
            _array = _length == 0 ? s_emptyArray : pool.Rent(length);
        }

        internal TempArray(T[] array, int length, ArrayPool<T> pool)
        {
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            _array = array ?? s_emptyArray;
            _length = array == null ? 0 : length;
            _pool = pool ?? ArrayPool<T>.Shared;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int index)
            => _array.AsSpan().CopyTo(array.AsSpan(index));

        private void ReturnArray(T[] replaceWith)
        {
            if (_array.IsNullOrEmpty() == false)
            {
                try
                {
                    _pool?.Return(_array, s_clearArray);
                }
                catch { }
            }

            _array = replaceWith ?? s_emptyArray;
        }

        public void Dispose()
        {
            ReturnArray(s_emptyArray);
            _length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        public ref struct Enumerator
        {
            private readonly TempArray<T> _array;
            private int _index;
            private T _current;

            public Enumerator(in TempArray<T> array)
            {
                _array = array;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                if (((uint)_index < (uint)_array.Length))
                {
                    _current = _array._array[_index];
                    _index++;
                    return true;
                }

                _index = _array.Length + 1;
                _current = default;
                return false;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current!;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }
        }
    }
}
