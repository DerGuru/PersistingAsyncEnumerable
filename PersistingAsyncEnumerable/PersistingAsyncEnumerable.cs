using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PersistingAsyncEnumerable
{
    public class PersistingAsyncEnumerable<T> : IEnumerable<T>, IAsyncEnumerable<T>, IAsyncDisposable, IDisposable
    {
        LinkedList<T> _storage = new LinkedList<T>();
        IAsyncEnumerable<T> _asyncEnumerable;
        IAsyncEnumerator<T> _asyncEnumerator;
        public PersistingAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
            _asyncEnumerator = _asyncEnumerable.GetAsyncEnumerator();
        }

        public async ValueTask DisposeAsync() => await _asyncEnumerator.DisposeAsync();

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new PersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

        public IEnumerator<T> GetEnumerator()
            => new PersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

        IEnumerator IEnumerable.GetEnumerator()
            => new PersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

        private MyRef _moving = new MyRef { Reference = 0 };
        internal class MyRef
        {
            public int Reference;
        }
    }

    public class PersistingAsyncEnumerator<T> : IAsyncEnumerator<T>, IEnumerator<T>
    {
        IAsyncEnumerator<T> _asyncEnumerator;
        LinkedList<T> _storage;
        LinkedListNode<T> _current;
        PersistingAsyncEnumerable<T>.MyRef _moving;

        internal PersistingAsyncEnumerator(IAsyncEnumerator<T> enumerator, LinkedList<T> storage, PersistingAsyncEnumerable<T>.MyRef moving)
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

        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }

        public bool MoveNext() => MoveNextAsync().GetAwaiter().GetResult();


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
            var last = _storage.Last;
            bool hasMoved = false;
            if (last == null || last == _current)
            {
                var moving = Interlocked.CompareExchange(ref _moving.Reference, 1, 0);
                if (moving == 0)
                {
                    try
                    {
                        hasMoved = await AddAsync();
                    }
                    finally
                    {
                        _moving.Reference = 0;
                    }
                }
                else
                {
                    while (_moving.Reference > 0)
                        await Task.Yield();
                    hasMoved = _current != _storage.Last;
                }
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

        private async Task<bool> AddAsync()
        {

            if (await _asyncEnumerator.MoveNextAsync())
            {
                _storage.AddLast(_asyncEnumerator.Current);
                return true;
            }
            else
                return false;
        }
    }
}
