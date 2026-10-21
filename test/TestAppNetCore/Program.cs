using Camefor.Tools.Cache;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestAppNetCore
{
    internal static class Program
    {
        private static async Task Main()
        {
            using (var cacheStore = new MemoryCacheStore())
            {
                var implementation = new SampleProvider();
                var provider = MemoryCacheProxy.Create<ISampleProvider>(implementation, cacheStore);

                await VerifyConcurrentAsyncCallsAsync(provider, implementation).ConfigureAwait(false);
                VerifySynchronousCalls(provider, implementation);
                await VerifyValueTaskCallsAsync(provider, implementation).ConfigureAwait(false);
                await VerifyDefaultValuesAreNotCachedAsync(provider, implementation).ConfigureAwait(false);
                await VerifyExceptionsAreNotCachedAsync(provider, implementation).ConfigureAwait(false);
            }

            await MemoryCachePrewarmTests.RunAsync().ConfigureAwait(false);

            Console.WriteLine("Memory cache checks passed.");
        }

        private static async Task VerifyConcurrentAsyncCallsAsync(
            ISampleProvider provider,
            SampleProvider implementation)
        {
            var calls = Enumerable.Range(0, 12)
                .Select(_ => provider.GetAsync("same-key", CancellationToken.None));
            var values = await Task.WhenAll(calls).ConfigureAwait(false);

            Ensure(values.All(value => value == "value:same-key"), "并发调用应返回相同结果。");
            Ensure(implementation.AsyncCallCount == 1, "同一缓存键的并发调用应只加载一次。");

            await provider.GetAsync("another-key", CancellationToken.None).ConfigureAwait(false);
            Ensure(implementation.AsyncCallCount == 2, "不同参数应生成不同缓存键。");
        }

        private static void VerifySynchronousCalls(ISampleProvider provider, SampleProvider implementation)
        {
            var first = provider.GetNumber(7);
            var second = provider.GetNumber(7);

            Ensure(first == second, "同步方法应返回缓存值。");
            Ensure(implementation.SyncCallCount == 1, "同步方法重复调用应只执行一次。");
        }

        private static async Task VerifyValueTaskCallsAsync(
            ISampleProvider provider,
            SampleProvider implementation)
        {
            var first = await provider.GetValueTaskAsync(9).ConfigureAwait(false);
            var second = await provider.GetValueTaskAsync(9).ConfigureAwait(false);

            Ensure(first == second, "ValueTask 方法应返回缓存值。");
            Ensure(implementation.ValueTaskCallCount == 1, "ValueTask 方法重复调用应只执行一次。");
        }

        private static async Task VerifyDefaultValuesAreNotCachedAsync(
            ISampleProvider provider,
            SampleProvider implementation)
        {
            await provider.GetOptionalAsync("missing").ConfigureAwait(false);
            await provider.GetOptionalAsync("missing").ConfigureAwait(false);

            Ensure(implementation.DefaultValueCallCount == 2, "默认返回值不应进入缓存。");
        }

        private static async Task VerifyExceptionsAreNotCachedAsync(
            ISampleProvider provider,
            SampleProvider implementation)
        {
            try
            {
                await provider.GetRecoverableAsync("retry-key").ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
            }

            var value = await provider.GetRecoverableAsync("retry-key").ConfigureAwait(false);
            Ensure(value == "recovered", "异常调用之后应允许重新加载。");
            Ensure(implementation.RecoverableCallCount == 2, "异常结果不应进入缓存。");
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    public interface ISampleProvider
    {
        [MemoryCache(expirationSeconds: 60)]
        Task<string> GetAsync(string key, [MemoryCacheKeyIgnore] CancellationToken cancellationToken);

        [MemoryCache(expirationSeconds: 60)]
        int GetNumber(int input);

        [MemoryCache(expirationSeconds: 60)]
        ValueTask<int> GetValueTaskAsync(int input);

        [MemoryCache(expirationSeconds: 60)]
        Task<string> GetOptionalAsync(string key);

        [MemoryCache(expirationSeconds: 60)]
        Task<string> GetRecoverableAsync(string key);
    }

    public sealed class SampleProvider : ISampleProvider
    {
        private int _asyncCallCount;
        private int _syncCallCount;
        private int _valueTaskCallCount;
        private int _defaultValueCallCount;
        private int _recoverableCallCount;

        public int AsyncCallCount => _asyncCallCount;

        public int SyncCallCount => _syncCallCount;

        public int ValueTaskCallCount => _valueTaskCallCount;

        public int DefaultValueCallCount => _defaultValueCallCount;

        public int RecoverableCallCount => _recoverableCallCount;

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _asyncCallCount);
            await Task.Delay(30, cancellationToken).ConfigureAwait(false);
            return "value:" + key;
        }

        public int GetNumber(int input)
        {
            Interlocked.Increment(ref _syncCallCount);
            return input * 2;
        }

        public ValueTask<int> GetValueTaskAsync(int input)
        {
            Interlocked.Increment(ref _valueTaskCallCount);
            return new ValueTask<int>(input * 3);
        }

        public Task<string> GetOptionalAsync(string key)
        {
            Interlocked.Increment(ref _defaultValueCallCount);
            return Task.FromResult<string>(null);
        }

        public Task<string> GetRecoverableAsync(string key)
        {
            if (Interlocked.Increment(ref _recoverableCallCount) == 1)
            {
                throw new InvalidOperationException("Temporary failure.");
            }

            return Task.FromResult("recovered");
        }
    }
}
