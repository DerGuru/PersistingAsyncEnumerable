﻿using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class ThreadSafePersistingAsyncEnumerator<T> : IAsyncEnumerator<T>, IEnumerator<T>
    {
        IAsyncEnumerator<T> _asyncEnumerator;
        LinkedList<T> _storage;
        LinkedListNode<T> _current;
        ThreadSafePersistingAsyncEnumerable<T>.MyRef _moving;

        internal ThreadSafePersistingAsyncEnumerator(IAsyncEnumerator<T> enumerator, LinkedList<T> storage, ThreadSafePersistingAsyncEnumerable<T>.MyRef moving)
        {
            _asyncEnumerator = enumerator;
            _storage = storage;
            _current = null;
            _moving = moving;
        }


        public T Current => _current.Value;

        object System.Collections.IEnumerator.Current => _current.Value;

        public void Dispose()
        {
            _current = null;
            _storage = null;
            _asyncEnumerator = null;
            _moving = null;
        }

        public ValueTask DisposeAsync() => new ValueTask(Task.Run(Dispose));


        public bool MoveNext()
        {
            var awaiter = MoveNextAsync().GetAwaiter();
            while (!awaiter.IsCompleted)
                Thread.Yield();
            return awaiter.GetResult();
        }


        public async ValueTask<bool> MoveNextAsync()
        {
            //before first load
            if (_storage.First == null)
                return await Move();

            //second round first access
            if (_current == null)
            {
                _current = _storage.First;
                return _current != null;
            }

            if (_current.Next != null)
            {
                //data already there
                _current = _current.Next;
                return true;
            }
            else
            {
                //data not yet there
                return await Move();
            }

        }

        private async ValueTask<bool> Move()
        {
            bool hasMoved = false;

            var moving = Interlocked.CompareExchange(ref _moving.Reference, 1, 0);
            if (moving == 0)
            {
                try
                {
                    hasMoved = await MoveAsyncEnum();
                }
                finally
                {
                    _moving.Reference = 0;
                }
            }
            else
            {
                while (_moving.Reference > 0)
                    await Task.Delay(30);
                hasMoved = _current != _storage.Last;
            }

            if (hasMoved)
            {
                if (_current == null)
                    _current = _storage.First;
                else
                    _current = GetNext(_storage.Last);
                return true;
            }
            else
                return false;
        }

        private async Task<bool> MoveAsyncEnum()
        {
            var hasMoved = await _asyncEnumerator.MoveNextAsync();
            if (hasMoved)
                _storage.AddLast(_asyncEnumerator.Current);
            return hasMoved;
        }

        private LinkedListNode<T> GetNext(LinkedListNode<T> last)
        {
            while (last != _current)
                last = last.Previous;
            return last.Next;
        }

        public void Reset()
        {
            _current = null;
        }

    }
}
