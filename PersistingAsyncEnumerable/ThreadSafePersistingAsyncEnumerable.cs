﻿using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    public class ThreadSafePersistingAsyncEnumerable<T> : IEnumerable<T>, IAsyncEnumerable<T>, IAsyncDisposable, IDisposable
    {
        internal class MyRef
        {
            public int Reference;
        }

        private LinkedList<T> _storage = new LinkedList<T>();
        private IAsyncEnumerable<T> _asyncEnumerable;
        private IAsyncEnumerator<T> _asyncEnumerator;
        private MyRef _moving = new MyRef { Reference = 0 };

        public ThreadSafePersistingAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
            _asyncEnumerator = _asyncEnumerable.GetAsyncEnumerator();
        }

        public async ValueTask DisposeAsync() => await _asyncEnumerator.DisposeAsync();

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();

        public virtual IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new ThreadSafePersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

        public virtual IEnumerator<T> GetEnumerator()
            => new ThreadSafePersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

        IEnumerator IEnumerable.GetEnumerator()
            => new ThreadSafePersistingAsyncEnumerator<T>(_asyncEnumerator, _storage, _moving);

    }
}
