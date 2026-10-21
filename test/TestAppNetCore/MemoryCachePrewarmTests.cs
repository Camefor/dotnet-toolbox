using Camefor.Tools.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestAppNetCore
{
    internal static class MemoryCachePrewarmTests
    {
        internal static async Task RunAsync()
        {
            await VerifyWarmupAndRefreshAsync().ConfigureAwait(false);
            VerifyParameterizedTargetIsRejected();
            VerifyManualCreationRequiresRegistry();
            await VerifyCriticalFailureFailsWarmupAsync().ConfigureAwait(false);
            await VerifyDependencyInjectionRegistrationAsync().ConfigureAwait(false);
        }

        private static async Task VerifyWarmupAndRefreshAsync()
        {
            using (var store = new MemoryCacheStore())
            {
                var registry = new MemoryCacheRegistry();
                var readiness = new MemoryCacheReadinessState();
                var implementation = new PrewarmProvider();
                var provider = MemoryCacheProxy.Create<IPrewarmProvider>(implementation, store, registry);

                Ensure(registry.Targets.Count == 3, "只有配置了 Warmup 或 RefreshSeconds 的方法应注册。");
                Ensure(
                    registry.Targets.All(target => target.Method.Name != nameof(IPrewarmProvider.GetIgnored)),
                    "MemoryCacheIgnore 标记的方法不应注册。");

                var warmup = new MemoryCacheWarmupHostedService(registry, readiness);
                await warmup.StartAsync(CancellationToken.None).ConfigureAwait(false);

                Ensure(readiness.IsReady, "预热完成后应标记就绪。");
                Ensure(implementation.CountCalls == 1, "预热应调用一次异步缓存方法。");
                Ensure(implementation.NameCalls == 1, "预热应调用一次同步缓存方法。");
                Ensure(implementation.ValueTaskCalls == 1, "预热应等待一次 ValueTask 缓存方法。");
                Ensure(implementation.IgnoredCalls == 0, "被忽略的方法不应参与预热。");

                var events = implementation.Events.ToList();
                var countEndIndex = events.IndexOf("count-end");
                var nameIndex = events.IndexOf("name");
                Ensure(
                    countEndIndex >= 0 && nameIndex > countEndIndex,
                    "Priority 较小的目标应先完成预热。");

                await provider.GetCountAsync().ConfigureAwait(false);
                await provider.GetValueTaskAsync().ConfigureAwait(false);
                Ensure(implementation.CountCalls == 1, "预热后的首次调用应命中缓存。");
                Ensure(implementation.ValueTaskCalls == 1, "ValueTask 预热后的首次调用应命中缓存。");

                using (var refresh = new MemoryCacheRefreshBackgroundService(
                    registry,
                    readiness,
                    null,
                    TimeSpan.Zero))
                {
                    await refresh.StartAsync(CancellationToken.None).ConfigureAwait(false);
                    Ensure(
                        implementation.Refreshed.Wait(TimeSpan.FromSeconds(5)),
                        "定时刷新应触发缓存回源。");
                    Ensure(implementation.CountCalls >= 2, "刷新后应重新调用缓存方法。");
                    await refresh.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private static void VerifyParameterizedTargetIsRejected()
        {
            using (var store = new MemoryCacheStore())
            {
                try
                {
                    MemoryCacheProxy.Create<IParameterizedProvider>(
                        new ParameterizedProvider(),
                        store,
                        new MemoryCacheRegistry());
                }
                catch (InvalidOperationException exception)
                {
                    Ensure(exception.Message.Contains("有参方法"), "有参预热目标应被明确拒绝。");
                    return;
                }
            }

            throw new InvalidOperationException("有参预热目标应被拒绝。");
        }

        private static void VerifyManualCreationRequiresRegistry()
        {
            using (var store = new MemoryCacheStore())
            {
                try
                {
                    MemoryCacheProxy.Create<IPrewarmProvider>(new PrewarmProvider(), store);
                }
                catch (InvalidOperationException exception)
                {
                    Ensure(
                        exception.Message.Contains("MemoryCacheRegistry"),
                        "缺少注册表时异常信息应给出明确指引。");
                    return;
                }
            }

            throw new InvalidOperationException("配置预热但未提供注册表时应立即失败。");
        }

        private static async Task VerifyCriticalFailureFailsWarmupAsync()
        {
            using (var store = new MemoryCacheStore())
            {
                var registry = new MemoryCacheRegistry();
                var readiness = new MemoryCacheReadinessState();
                MemoryCacheProxy.Create<ICriticalProvider>(new CriticalProvider(), store, registry);
                var warmup = new MemoryCacheWarmupHostedService(registry, readiness);

                try
                {
                    await warmup.StartAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (InvalidOperationException exception)
                {
                    Ensure(exception.Message == "critical failure", "Critical 预热失败应抛出原始异常。");
                    Ensure(!readiness.IsReady, "Critical 预热失败后不应标记就绪。");
                    return;
                }
            }

            throw new InvalidOperationException("Critical 预热失败应终止启动。");
        }

        private static async Task VerifyDependencyInjectionRegistrationAsync()
        {
            Interlocked.Exchange(ref DiPrewarmProvider.CallCount, 0);

            // 只调用代理注册，验证预热/刷新宿主服务会自动完成装配。
            var services = new ServiceCollection();
            services.AddMemoryCacheProxy<IDiPrewarmProvider, DiPrewarmProvider>();

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
                Ensure(
                    hostedServices.Count == 3,
                    "仅注册代理时也应自动注册初始化、预热和刷新三个宿主服务。");

                var initialization = hostedServices
                    .OfType<MemoryCacheProxyInitializationHostedService>()
                    .Single();
                var warmup = hostedServices
                    .OfType<MemoryCacheWarmupHostedService>()
                    .Single();

                await initialization.StartAsync(CancellationToken.None).ConfigureAwait(false);
                await warmup.StartAsync(CancellationToken.None).ConfigureAwait(false);

                Ensure(DiPrewarmProvider.CallCount == 1, "DI 注册的代理应在预热时完成首次加载。");

                var provider = serviceProvider.GetRequiredService<IDiPrewarmProvider>();
                await provider.GetValueAsync().ConfigureAwait(false);
                Ensure(DiPrewarmProvider.CallCount == 1, "预热后的调用应命中缓存。");

                var readiness = serviceProvider.GetRequiredService<MemoryCacheReadinessState>();
                Ensure(readiness.IsReady, "DI 预热完成后应标记就绪。");
            }
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    public interface IPrewarmProvider
    {
        [MemoryCache(expirationSeconds: 60, Warmup = true, RefreshSeconds = 1, Priority = 0)]
        Task<int> GetCountAsync();

        [MemoryCache(expirationSeconds: 60, Warmup = true, Priority = 10)]
        string GetName();

        [MemoryCache(expirationSeconds: 60, Warmup = true, Priority = 10)]
        ValueTask<int> GetValueTaskAsync();

        [MemoryCache(expirationSeconds: 60, Warmup = true)]
        [MemoryCacheIgnore]
        string GetIgnored();
    }

    public sealed class PrewarmProvider : IPrewarmProvider
    {
        private readonly List<string> _events = new List<string>();
        private int _countCalls;
        private int _nameCalls;
        private int _valueTaskCalls;
        private int _ignoredCalls;

        public ManualResetEventSlim Refreshed { get; } = new ManualResetEventSlim(false);

        public int CountCalls
        {
            get { return Volatile.Read(ref _countCalls); }
        }

        public int NameCalls
        {
            get { return Volatile.Read(ref _nameCalls); }
        }

        public int ValueTaskCalls
        {
            get { return Volatile.Read(ref _valueTaskCalls); }
        }

        public int IgnoredCalls
        {
            get { return Volatile.Read(ref _ignoredCalls); }
        }

        public IReadOnlyList<string> Events
        {
            get
            {
                lock (_events)
                {
                    return _events.ToArray();
                }
            }
        }

        public async Task<int> GetCountAsync()
        {
            var calls = Interlocked.Increment(ref _countCalls);
            Record("count-start");
            if (calls > 1)
            {
                Refreshed.Set();
            }

            await Task.Delay(30).ConfigureAwait(false);
            Record("count-end");
            return calls;
        }

        public string GetName()
        {
            Interlocked.Increment(ref _nameCalls);
            Record("name");
            return "name";
        }

        public ValueTask<int> GetValueTaskAsync()
        {
            Interlocked.Increment(ref _valueTaskCalls);
            return new ValueTask<int>(99);
        }

        public string GetIgnored()
        {
            Interlocked.Increment(ref _ignoredCalls);
            return "ignored";
        }

        private void Record(string value)
        {
            lock (_events)
            {
                _events.Add(value);
            }
        }
    }

    public interface IParameterizedProvider
    {
        [MemoryCache(expirationSeconds: 60, Warmup = true)]
        string Get(string key);
    }

    public sealed class ParameterizedProvider : IParameterizedProvider
    {
        public string Get(string key)
        {
            return key;
        }
    }

    public interface ICriticalProvider
    {
        [MemoryCache(expirationSeconds: 60, Warmup = true, Critical = true)]
        Task<string> GetAsync();
    }

    public sealed class CriticalProvider : ICriticalProvider
    {
        public Task<string> GetAsync()
        {
            throw new InvalidOperationException("critical failure");
        }
    }

    public interface IDiPrewarmProvider
    {
        [MemoryCache(expirationSeconds: 60, Warmup = true)]
        Task<int> GetValueAsync();
    }

    public sealed class DiPrewarmProvider : IDiPrewarmProvider
    {
        public static int CallCount;

        public async Task<int> GetValueAsync()
        {
            Interlocked.Increment(ref CallCount);
            await Task.Delay(10).ConfigureAwait(false);
            return 42;
        }
    }
}
