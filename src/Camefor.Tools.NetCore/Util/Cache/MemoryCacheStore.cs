using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 存储进程内缓存项，并合并同一缓存键的并发加载请求。
    /// </summary>
    public sealed class MemoryCacheStore : IDisposable
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);
        private readonly ConcurrentDictionary<string, CacheEntry> _entries =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);
        private readonly Timer _cleanupTimer;
        private int _disposed;

        /// <summary>
        /// 创建独立的内存缓存容器。
        /// </summary>
        public MemoryCacheStore()
        {
            _cleanupTimer = new Timer(RemoveExpiredEntries, null, CleanupInterval, CleanupInterval);
        }

        /// <summary>
        /// 获取所有默认代理共享的缓存容器。
        /// </summary>
        public static MemoryCacheStore Shared { get; } = new MemoryCacheStore();

        /// <summary>
        /// 获取当前缓存项数量。
        /// </summary>
        public int Count
        {
            get { return _entries.Count; }
        }

        /// <summary>
        /// 清空当前容器中的所有缓存项。
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            _entries.Clear();
        }

        /// <summary>
        /// 释放当前缓存容器使用的清理计时器并清空缓存。
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _cleanupTimer.Dispose();
            _entries.Clear();
        }

        internal async Task<object> GetOrCreateAsync(
            string key,
            TimeSpan expiration,
            Func<Task<object>> valueFactory,
            Func<object, bool> shouldCache)
        {
            ThrowIfDisposed();

            while (true)
            {
                CacheEntry entry;
                if (_entries.TryGetValue(key, out entry))
                {
                    if (!entry.IsExpired(GetUtcTicks()))
                    {
                        return await AwaitEntryAsync(key, entry, expiration, shouldCache).ConfigureAwait(false);
                    }

                    RemoveEntry(key, entry);
                }

                var newEntry = new CacheEntry(valueFactory);
                if (_entries.TryAdd(key, newEntry))
                {
                    return await AwaitEntryAsync(key, newEntry, expiration, shouldCache).ConfigureAwait(false);
                }
            }
        }

        private async Task<object> AwaitEntryAsync(
            string key,
            CacheEntry entry,
            TimeSpan expiration,
            Func<object, bool> shouldCache)
        {
            try
            {
                var value = await entry.GetValueAsync().ConfigureAwait(false);
                if (shouldCache(value))
                {
                    // 有效期从数据加载完成后开始，避免慢请求在返回前就使缓存过期。
                    entry.TrySetExpiration(GetUtcTicks() + expiration.Ticks);
                }
                else
                {
                    RemoveEntry(key, entry);
                }

                return value;
            }
            catch
            {
                // 异常或取消结果不进入缓存，让下一次调用可以重新加载。
                RemoveEntry(key, entry);
                throw;
            }
        }

        private void RemoveExpiredEntries(object state)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            var utcTicks = GetUtcTicks();
            foreach (var pair in _entries)
            {
                if (pair.Value.IsExpired(utcTicks))
                {
                    RemoveEntry(pair.Key, pair.Value);
                }
            }
        }

        private void RemoveEntry(string key, CacheEntry entry)
        {
            // 精确删除旧实例，避免过期清理误删同一键下刚创建的新缓存项。
            ((ICollection<KeyValuePair<string, CacheEntry>>)_entries)
                .Remove(new KeyValuePair<string, CacheEntry>(key, entry));
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(MemoryCacheStore));
            }
        }

        private static long GetUtcTicks()
        {
            return DateTime.UtcNow.Ticks;
        }

        private sealed class CacheEntry
        {
            private readonly Lazy<Task<object>> _value;
            private long _expirationUtcTicks = long.MaxValue;

            internal CacheEntry(Func<Task<object>> valueFactory)
            {
                _value = new Lazy<Task<object>>(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication);
            }

            internal Task<object> GetValueAsync()
            {
                return _value.Value;
            }

            internal bool IsExpired(long utcTicks)
            {
                return utcTicks >= Interlocked.Read(ref _expirationUtcTicks);
            }

            internal void TrySetExpiration(long utcTicks)
            {
                // 仅首次加载完成时写入绝对过期时间，缓存命中不会延长有效期。
                Interlocked.CompareExchange(ref _expirationUtcTicks, utcTicks, long.MaxValue);
            }
        }
    }
}
