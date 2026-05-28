using System;
using System.Diagnostics;

namespace Camefor.Tools.Util
{
    /// <summary>
    /// 描   述  ： 性能测试 https://stackoverflow.com/questions/13681664/helper-class-for-performance-tests-using-stopwatch-class#
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ：                                   
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                   
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 
    public class PerformanceTester : IDisposable
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private Action<TimeSpan> _callback;

        public PerformanceTester()
        {
            _stopwatch.Start();
        }

        public PerformanceTester(Action<TimeSpan> callback) : this()
        {
            _callback = callback;
        }

        public static PerformanceTester Start(Action<TimeSpan> callback)
        {
            return new PerformanceTester(callback);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            if (_callback != null)
                _callback(Result);
        }

        public TimeSpan Result
        {
            get { return _stopwatch.Elapsed; }
        }
    }
}
