using System;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 指示缓存键生成器忽略指定方法参数。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class MemoryCacheKeyIgnoreAttribute : Attribute
    {
    }
}
