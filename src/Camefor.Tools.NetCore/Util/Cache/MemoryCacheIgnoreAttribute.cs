using System;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 显式排除标记：方法虽然带有 <see cref="MemoryCacheAttribute"/>，
    /// 但不参与自动预热与定时刷新。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MemoryCacheIgnoreAttribute : Attribute
    {
    }
}
