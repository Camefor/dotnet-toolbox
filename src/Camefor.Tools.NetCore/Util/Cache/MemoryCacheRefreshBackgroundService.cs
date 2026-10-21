using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 缓存后台定时刷新服务。
    /// 为每个配置了 <see cref="MemoryCacheAttribute.RefreshSeconds"/> 的目标启动独立刷新循环：
    /// 先移除旧缓存，再调用代理方法触发回源并写回新值。
    /// 每个目标启动时会随机错峰，避免多个目标同时回源冲击数据库。
    /// </summary>
    public sealed class MemoryCacheRefreshBackgroundService : BackgroundService
    {
        private static readonly TimeSpan DefaultStartupJitter = TimeSpan.FromSeconds(10);
        private static readonly Random JitterRandom = new Random();

        private readonly MemoryCacheRegistry _registry;
        private readonly MemoryCacheReadinessState _readiness;
        private readonly ILogger<MemoryCacheRefreshBackgroundService> _logger;
        private readonly TimeSpan _startupJitter;

        /// <summary>
        /// 创建刷新服务。
        /// </summary>
        public MemoryCacheRefreshBackgroundService(
            MemoryCacheRegistry registry,
            MemoryCacheReadinessState readiness)
            : this(registry, readiness, null, DefaultStartupJitter)
        {
        }

        /// <summary>
        /// 创建刷新服务。
        /// </summary>
        public MemoryCacheRefreshBackgroundService(
            MemoryCacheRegistry registry,
            MemoryCacheReadinessState readiness,
            ILogger<MemoryCacheRefreshBackgroundService> logger)
            : this(registry, readiness, logger, DefaultStartupJitter)
        {
        }

        /// <summary>
        /// 创建刷新服务，并指定启动错峰的最大时长。
        /// </summary>
        public MemoryCacheRefreshBackgroundService(
            MemoryCacheRegistry registry,
            MemoryCacheReadinessState readiness,
            ILogger<MemoryCacheRefreshBackgroundService> logger,
            TimeSpan startupJitter)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
            _logger = logger ?? NullLogger<MemoryCacheRefreshBackgroundService>.Instance;
            _startupJitter = startupJitter < TimeSpan.Zero ? TimeSpan.Zero : startupJitter;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var targets = _registry.Targets
                .Where(target => target.Attribute.RefreshSeconds > 0)
                .ToList();

            if (targets.Count == 0)
            {
                _logger.LogInformation("MemoryCache 刷新服务：无目标，跳过启动。");
                return Task.CompletedTask;
            }

            foreach (var target in targets)
            {
                if (target.Attribute.RefreshSeconds >= target.Attribute.ExpirationSeconds)
                {
                    _logger.LogWarning(
                        "MemoryCache 配置可疑：{Target} 的 RefreshSeconds({RefreshSeconds}s) >= ExpirationSeconds({ExpirationSeconds}s)，" +
                        "刷新未在缓存到期前完成会出现失效窗口。",
                        target.DisplayName,
                        target.Attribute.RefreshSeconds,
                        target.Attribute.ExpirationSeconds);
                }
            }

            _logger.LogInformation("MemoryCache 刷新服务启动，共 {Count} 个目标。", targets.Count);
            return Task.WhenAll(targets.Select(target => RunLoopAsync(target, stoppingToken)).ToArray());
        }

        private async Task RunLoopAsync(MemoryCacheTarget target, CancellationToken cancellationToken)
        {
            // 存在预热目标时，等待预热完成后再开始刷新，避免与预热撞车。
            if (_registry.Targets.Any(item => item.Attribute.Warmup))
            {
                while (!_readiness.IsReady && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var jitterMilliseconds = GetJitterMilliseconds();
            if (jitterMilliseconds > 0)
            {
                try
                {
                    await Task.Delay(jitterMilliseconds, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            var interval = TimeSpan.FromSeconds(target.Attribute.RefreshSeconds);
            var schedule = Stopwatch.StartNew();
            var nextDue = interval;

            while (!cancellationToken.IsCancellationRequested)
            {
                // 使用 Stopwatch 的单调时钟计算下一次触发时间，避免系统时间调整影响刷新周期。
                var delay = nextDue - schedule.Elapsed;
                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }

                // 按计划时间推进，避免刷新耗时导致周期漂移；落后过多时直接跳到下一个周期。
                nextDue = nextDue + interval;
                if (nextDue <= schedule.Elapsed)
                {
                    nextDue = schedule.Elapsed + interval;
                }

                await RefreshOnceAsync(target, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshOnceAsync(MemoryCacheTarget target, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 先移除旧值，再通过代理方法回源；期间并发请求会共享同一次加载，不会打穿缓存。
                target.CacheStore.Remove(target.CacheKey);
                await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _logger.LogDebug(
                    "MemoryCache 刷新 {Target} 完成，耗时 {ElapsedMs}ms。",
                    target.DisplayName,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 宿主停止信号，正常退出循环。
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                _logger.LogError(
                    exception,
                    "MemoryCache 刷新 {Target} 失败，缓存已移除，等待下次调用懒加载或下次刷新。耗时 {ElapsedMs}ms。",
                    target.DisplayName,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private int GetJitterMilliseconds()
        {
            var maxMilliseconds = (int)Math.Min(_startupJitter.TotalMilliseconds, int.MaxValue);
            if (maxMilliseconds <= 0)
            {
                return 0;
            }

            lock (JitterRandom)
            {
                return JitterRandom.Next(0, maxMilliseconds);
            }
        }
    }
}
