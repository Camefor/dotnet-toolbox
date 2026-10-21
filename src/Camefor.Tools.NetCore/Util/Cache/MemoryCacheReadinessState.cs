using System.Threading;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 缓存预热就绪状态。
    /// 预热完成后置为 true，可用于健康检查或后台刷新服务的启动同步。
    /// 使用 <see cref="Interlocked"/> 保证多线程下的可见性。
    /// </summary>
    public sealed class MemoryCacheReadinessState
    {
        private int _isReady;

        /// <summary>
        /// 获取或设置预热是否已完成。
        /// </summary>
        public bool IsReady
        {
            get { return Interlocked.CompareExchange(ref _isReady, 0, 0) == 1; }
            set { Interlocked.Exchange(ref _isReady, value ? 1 : 0); }
        }
    }
}
