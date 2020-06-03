using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class AutoLoadingPersistingAsyncEnumerable<T> : IEnumerable<T>, IAsyncEnumerable<T>, IAsyncDisposable, IDisposable
    {
        internal class MyRef
        {
            public int Reference = 0;
            public bool FinishedLoading { get; set; } = false;
        }

        private LinkedList<T> _storage = new LinkedList<T>();
        private IAsyncEnumerable<T> _asyncEnumerable;
        private MyRef _moving = new MyRef();

        public AutoLoadingPersistingAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
            Task.Run(Load);
        }

        private async Task Load()
        {
            await foreach (T item in _asyncEnumerable)
            {
                _storage.AddLast(item);
            }
            _moving.FinishedLoading = true;
        }

        public ValueTask DisposeAsync() => new ValueTask(Task.Run(Dispose));

        public void Dispose() 
        {
            _storage = null;
            _asyncEnumerable = null;
        }

        public virtual IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AutoLoadingPersistingAsyncEnumerator<T>(_storage, _moving);

        public virtual IEnumerator<T> GetEnumerator()
            => new AutoLoadingPersistingAsyncEnumerator<T>(_storage, _moving);

        IEnumerator IEnumerable.GetEnumerator()
            => new AutoLoadingPersistingAsyncEnumerator<T>(_storage, _moving);

    }
}
