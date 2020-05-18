using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// wrappes an IAsyncEnumerable and persists the data as it is loaded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleThreadPersistingAsyncEnumerable<T> : IEnumerable<T>, IAsyncEnumerable<T>, IAsyncDisposable, IDisposable
    {
        private LinkedList<T> _storage = new LinkedList<T>();
        private IAsyncEnumerable<T> _asyncEnumerable;
        private IAsyncEnumerator<T> _asyncEnumerator;
        private SingleThreadPersistingAsyncEnumerator<T> _currentEnumerator = null;

        public SingleThreadPersistingAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
            _asyncEnumerator = _asyncEnumerable.GetAsyncEnumerator();

        }

        public async ValueTask DisposeAsync() => await _asyncEnumerator.DisposeAsync();

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetNewEnumerator();

        public IEnumerator<T> GetEnumerator() => GetNewEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetNewEnumerator();

        private SingleThreadPersistingAsyncEnumerator<T> GetNewEnumerator()
        {
            if (_currentEnumerator == null || _currentEnumerator.IsDisposed)
                return new SingleThreadPersistingAsyncEnumerator<T>(_asyncEnumerator, _storage);

            throw new InvalidOperationException("Enumerator already in use. Dispose first, before making another.");
        }

    }
}
