using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class SingleThreadedPersistingAsyncEnumerator<T> : IAsyncEnumerator<T>, IEnumerator<T>
    {
        IAsyncEnumerator<T> _asyncEnumerator;
        LinkedList<T> _storage;
        LinkedListNode<T> _current;

        internal SingleThreadedPersistingAsyncEnumerator(IAsyncEnumerator<T> enumerator, LinkedList<T> storage)
        {
            _asyncEnumerator = enumerator;
            _storage = storage;
            _current = null;
        }

        public T Current => _current.Value;

        object System.Collections.IEnumerator.Current => _current.Value;

        public void Dispose(){}

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
