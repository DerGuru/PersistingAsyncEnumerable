using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Persisting Async Enumerator for Single Threaded Access
    /// </summary>
    /// <typeparam name="T">Contained Element's Type</typeparam>
    public class SingleThreadPersistingAsyncEnumerator<T> : IAsyncEnumerator<T>, IEnumerator<T>
    {
        IAsyncEnumerator<T> _asyncEnumerator;
        LinkedList<T> _storage;
        LinkedListNode<T> _current;

        public SingleThreadPersistingAsyncEnumerator(IAsyncEnumerator<T> enumerator, LinkedList<T> storage)
        {
            _asyncEnumerator = enumerator;
            _storage = storage;
            _current = null;
        }

        public T Current => _current.Value;

        object System.Collections.IEnumerator.Current => _current.Value;

        public bool IsDisposed
        { get; private set; } = false;

        public void Dispose()
        {
            _current = null;
            _storage = null;
            _asyncEnumerator = null;
            IsDisposed = true;
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
            
            if (_current.Next != null) //data is already there
            {  

                _current = _current.Next;
                return true;
            }
            else //data is not yet there
            {
                
                return await Move();
            }

        }

        private async ValueTask<bool> Move()
        {
            bool hasMoved = await _asyncEnumerator.MoveNextAsync();
            if (hasMoved)
            {
                _storage.AddLast(_asyncEnumerator.Current);
                _current = _storage.Last;
            }
            return hasMoved;
        }

        public void Reset()
        {
            _current = null;
        }

    }
}
