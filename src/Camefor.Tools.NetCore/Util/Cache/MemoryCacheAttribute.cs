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
    }
}
