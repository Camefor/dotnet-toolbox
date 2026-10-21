using System;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 标记需要按方法参数缓存返回值的方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MemoryCacheAttribute : Attribute
    {
        private int _expirationSeconds;

        /// <summary>
        /// 创建内存缓存标记。
        /// </summary>
        /// <param name="expirationSeconds">缓存有效期，单位为秒。</param>
        /// <param name="cacheDefaultValues">是否缓存返回类型的默认值，例如 null、0 或 false。</param>
        public MemoryCacheAttribute(int expirationSeconds = 300, bool cacheDefaultValues = false)
        {
            ExpirationSeconds = expirationSeconds;
            CacheDefaultValues = cacheDefaultValues;
        }

        /// <summary>
        /// 获取或设置缓存有效期，单位为秒。
        /// </summary>
        public int ExpirationSeconds
        {
            get { return _expirationSeconds; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(ExpirationSeconds), "缓存有效期必须大于 0 秒。");
                }

                _expirationSeconds = value;
            }
        }

        /// <summary>
        /// 获取或设置是否缓存返回类型的默认值。
        /// </summary>
        public bool CacheDefaultValues { get; set; }

        /// <summary>
        /// 获取或设置是否参与启动预热。仅支持无参方法。
        /// </summary>
        public bool Warmup { get; set; }

        /// <summary>
        /// 获取或设置后台定时刷新间隔，单位为秒。小于等于 0 表示不刷新。
        /// 建议小于 <see cref="ExpirationSeconds"/>，使刷新在缓存到期前完成。
        /// </summary>
        public int RefreshSeconds { get; set; }

        /// <summary>
        /// 获取或设置预热与刷新的优先级，数值越小越先执行。
        /// 预热阶段同优先级并发，不同优先级串行。
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// 获取或设置预热失败时是否终止启动。
        /// true 表示向宿主抛出异常；false 表示仅记录错误日志并继续启动。
        /// </summary>
        public bool Critical { get; set; }
    }
}
