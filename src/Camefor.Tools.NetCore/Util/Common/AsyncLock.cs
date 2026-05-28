using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Camefor.Tools.Util.Common
{
    /// <summary>
    /// 异步锁
    /// https://mp.weixin.qq.com/s/WLKhMpaj9wRe-TeUioki4Q
    /// </summary>

    public sealed class AsyncLock : IDisposable
    {
        public readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly Task<Release> _cachedReleaseTask;

        public bool _disposed;

        public AsyncLock()
        {
            _cachedReleaseTask = Task.FromResult(new Release(this));
        }

        public Task<Release> LockAsync()
        {
            var waitTask = _semaphore.WaitAsync();
            return waitTask.IsCompleted
               ? _cachedReleaseTask
               : LockAsyncInternal(waitTask);
        }

        private async Task<Release> LockAsyncInternal(Task waitTask)
        {
            await waitTask.ConfigureAwait(false);
            return new Release(this);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }

    public readonly struct Release : IDisposable
    {
        private readonly AsyncLock _asyncLock;

        internal Release(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock;
        }

        public void Dispose()
        {
            if (_asyncLock != null && !_asyncLock._disposed)
            {
                _asyncLock._semaphore.Release();
            }
        }
    }

}
