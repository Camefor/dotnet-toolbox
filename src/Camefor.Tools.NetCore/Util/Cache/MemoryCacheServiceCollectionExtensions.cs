using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 缓存代理与预热/刷新框架的依赖注入注册扩展。
    /// </summary>
    public static class MemoryCacheServiceCollectionExtensions
    {
        /// <summary>
        /// 注册缓存共享容器、注册表、就绪状态，以及代理初始化、启动预热和后台定时刷新服务。
        /// 使用 <see cref="AddMemoryCacheProxy{TService,TImplementation}"/> 时会自动调用本方法。
        /// </summary>
        public static IServiceCollection AddMemoryCacheProxyFramework(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            EnsureCoreServices(services);
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, MemoryCacheProxyInitializationHostedService>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, MemoryCacheWarmupHostedService>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, MemoryCacheRefreshBackgroundService>());
            return services;
        }

        /// <summary>
        /// 将接口实现注册为单例缓存代理，并自动注册预热与后台定时刷新服务。
        /// 实现类型及其依赖会随代理成为单例，请不要依赖 Scoped 服务。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <typeparam name="TImplementation">实现类型。</typeparam>
        public static IServiceCollection AddMemoryCacheProxy<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // 单次注册即可完成全部装配，避免使用者忘记单独注册预热/刷新宿主服务。
            services.AddMemoryCacheProxyFramework();
            services.AddSingleton<TService>(serviceProvider =>
            {
                var cacheStore = serviceProvider.GetRequiredService<MemoryCacheStore>();
                var registry = serviceProvider.GetRequiredService<MemoryCacheRegistry>();
                var implementation = ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
                return MemoryCacheProxy.Create<TService>(implementation, cacheStore, registry);
            });
            services.AddSingleton(new MemoryCacheProxyServiceType(typeof(TService)));
            return services;
        }

        private static void EnsureCoreServices(IServiceCollection services)
        {
            services.TryAddSingleton<MemoryCacheStore>();
            services.TryAddSingleton<MemoryCacheRegistry>();
            services.TryAddSingleton<MemoryCacheReadinessState>();
        }
    }

    /// <summary>
    /// 记录一个已注册的缓存代理服务类型，用于启动期提前解析代理并填充缓存目标注册表。
    /// </summary>
    public sealed class MemoryCacheProxyServiceType
    {
        /// <summary>
        /// 创建服务类型记录。
        /// </summary>
        /// <param name="serviceType">代理注册的服务接口类型。</param>
        public MemoryCacheProxyServiceType(Type serviceType)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        }

        /// <summary>
        /// 获取代理注册的服务接口类型。
        /// </summary>
        public Type ServiceType { get; }
    }

    /// <summary>
    /// 启动期解析所有已注册的缓存代理，确保 <see cref="MemoryCacheRegistry"/> 在预热开始前完成填充。
    /// </summary>
    public sealed class MemoryCacheProxyInitializationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<MemoryCacheProxyServiceType> _serviceTypes;

        /// <summary>
        /// 创建代理初始化服务。
        /// </summary>
        public MemoryCacheProxyInitializationHostedService(
            IServiceProvider serviceProvider,
            IEnumerable<MemoryCacheProxyServiceType> serviceTypes)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _serviceTypes = serviceTypes ?? Array.Empty<MemoryCacheProxyServiceType>();
        }

        /// <summary>
        /// 解析所有缓存代理，触发目标注册。
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var serviceType in _serviceTypes)
            {
                _serviceProvider.GetRequiredService(serviceType.ServiceType);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 停止时无需额外处理。
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
