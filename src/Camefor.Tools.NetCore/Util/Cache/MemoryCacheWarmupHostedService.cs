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
    /// 启动期缓存预热服务。
    /// 按 <see cref="MemoryCacheAttribute.Priority"/> 分组：同优先级并发、不同优先级串行，
    /// 全部完成后置位 <see cref="MemoryCacheReadinessState.IsReady"/>。
    /// 标记为 <see cref="MemoryCacheAttribute.Critical"/> 的目标预热失败时抛出异常，终止宿主启动。
    /// </summary>
    public sealed class MemoryCacheWarmupHostedService : IHostedService
    {
        private readonly MemoryCacheRegistry _registry;
        private readonly MemoryCacheReadinessState _readiness;
        private readonly ILogger<MemoryCacheWarmupHostedService> _logger;

        /// <summary>
        /// 创建预热服务。
        /// </summary>
        public MemoryCacheWarmupHostedService(
            MemoryCacheRegistry registry,
            MemoryCacheReadinessState readiness)
            : this(registry, readiness, null)
        {
        }

        /// <summary>
        /// 创建预热服务。
        /// </summary>
        public MemoryCacheWarmupHostedService(
            MemoryCacheRegistry registry,
            MemoryCacheReadinessState readiness,
            ILogger<MemoryCacheWarmupHostedService> logger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
            _logger = logger ?? NullLogger<MemoryCacheWarmupHostedService>.Instance;
        }

        /// <summary>
        /// 启动宿主时执行预热。
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return WarmupAsync(cancellationToken);
        }

        /// <summary>
        /// 停止宿主时无需额外处理。
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行一次预热。
        /// </summary>
        public async Task WarmupAsync(CancellationToken cancellationToken = default)
        {
            var targets = _registry.Targets.Where(target => target.Attribute.Warmup).ToList();
            _readiness.IsReady = false;

            if (targets.Count == 0)
            {
                _readiness.IsReady = true;
                _logger.LogInformation("MemoryCache 预热：无目标，直接标记就绪。");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("MemoryCache 预热开始，共 {Count} 个目标。", targets.Count);

            var groups = targets
                .GroupBy(target => target.Attribute.Priority)
                .OrderBy(group => group.Key)
                .ToList();

            foreach (var group in groups)
            {
                var tasks = group
                    .Select(target => WarmupOneAsync(target, cancellationToken))
                    .ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _readiness.IsReady = true;
            _logger.LogInformation(
                "MemoryCache 预热完成，耗时 {ElapsedMs}ms，目标数 {Count}。",
                stopwatch.ElapsedMilliseconds,
                targets.Count);
        }

        private async Task WarmupOneAsync(MemoryCacheTarget target, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();
                _logger.LogInformation(
                    "MemoryCache 预热 {Target} 完成，耗时 {ElapsedMs}ms。",
                    target.DisplayName,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException))
            {
                stopwatch.Stop();
                if (target.Attribute.Critical)
                {
                    _logger.LogError(
                        exception,
                        "MemoryCache 预热 {Target} 失败且被标记 Critical，进程将终止。耗时 {ElapsedMs}ms。",
                        target.DisplayName,
                        stopwatch.ElapsedMilliseconds);
                    throw;
                }

                _logger.LogError(
                    exception,
                    "MemoryCache 预热 {Target} 失败（非 Critical，继续启动，后续走懒加载/刷新兜底）。耗时 {ElapsedMs}ms。",
                    target.DisplayName,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
