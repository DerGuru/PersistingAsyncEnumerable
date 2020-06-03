using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class AutoLoadingPersistingAsyncEnumerator<T> : IAsyncEnumerator<T>, IEnumerator<T>
    {
       
        LinkedList<T> _storage;
        LinkedListNode<T> _current;
        AutoLoadingPersistingAsyncEnumerable<T>.MyRef _state;

        internal AutoLoadingPersistingAsyncEnumerator(LinkedList<T> storage, AutoLoadingPersistingAsyncEnumerable<T>.MyRef state)
        {
           
            _storage = storage;
            _current = null;
            _state = state;
        }

        public T Current => _current.Value;

        object System.Collections.IEnumerator.Current => _current.Value;

        public void Dispose()
        {
            _current = null;
            _storage = null;
            _state = null;
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
            if (_storage.First == null || _current == null)
                return await LoadFirst();

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

        private async Task<bool> LoadFirst()
        {
            while (_storage.First == null && !_state.FinishedLoading)
            {
                await Task.Delay(30);
            }
            _current = _storage.First;
            return _current != null;
        }

        private async Task<bool> Move()
        {
            while (_storage.Last == _current && !_state.FinishedLoading)
            {
                await Task.Delay(30);
            }
            if (_storage.Last == _current)
                return false;
            else
            {
                _current = GetNext(_storage.Last);
                return true;
            }
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
