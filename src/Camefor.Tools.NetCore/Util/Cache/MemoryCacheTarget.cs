using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 描述一个参与预热/定时刷新框架的缓存方法。
    /// 由 <see cref="MemoryCacheProxy"/> 创建代理时一次性生成。
    /// </summary>
    public sealed class MemoryCacheTarget
    {
        private readonly Func<CancellationToken, Task> _invoker;

        internal MemoryCacheTarget(
            Type serviceType,
            Type implementationType,
            MethodInfo method,
            MemoryCacheAttribute attribute,
            string cacheKey,
            MemoryCacheStore cacheStore,
            Func<CancellationToken, Task> invoker)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Method = method;
            Attribute = attribute;
            CacheKey = cacheKey;
            CacheStore = cacheStore;
            _invoker = invoker;
        }

        /// <summary>
        /// 获取代理注册到容器中的服务接口类型。
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// 获取真实实现类型。
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// 获取带缓存特性的接口方法。
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// 获取方法上的缓存特性配置。
        /// </summary>
        public MemoryCacheAttribute Attribute { get; }

        /// <summary>
        /// 获取无参方法对应的完整缓存键。
        /// </summary>
        public string CacheKey { get; }

        /// <summary>
        /// 获取该目标使用的缓存容器。
        /// </summary>
        public MemoryCacheStore CacheStore { get; }

        /// <summary>
        /// 获取用于日志展示的名称。
        /// </summary>
        public string DisplayName
        {
            get { return ServiceType.Name + "." + Method.Name; }
        }

        internal Task InvokeAsync(CancellationToken cancellationToken)
        {
            return _invoker(cancellationToken);
        }
    }

    /// <summary>
    /// 收集所有参与预热/定时刷新的缓存目标。
    /// </summary>
    public sealed class MemoryCacheRegistry
    {
        private readonly object _syncRoot = new object();
        private readonly List<MemoryCacheTarget> _targets = new List<MemoryCacheTarget>();
        private readonly HashSet<Tuple<MemoryCacheStore, string>> _registeredKeys =
            new HashSet<Tuple<MemoryCacheStore, string>>();

        /// <summary>
        /// 获取当前已注册的目标快照。
        /// </summary>
        public IReadOnlyList<MemoryCacheTarget> Targets
        {
            get
            {
                lock (_syncRoot)
                {
                    return _targets.ToArray();
                }
            }
        }

        internal void Add(MemoryCacheTarget target)
        {
            var identity = Tuple.Create(target.CacheStore, target.CacheKey);
            lock (_syncRoot)
            {
                // 同一个缓存容器下的同一个键只注册一次，避免重复预热和重复刷新。
                if (_registeredKeys.Add(identity))
                {
                    _targets.Add(target);
                }
            }
        }
    }
}
