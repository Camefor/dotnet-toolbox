using System;
using System.IO;

namespace Camefor.Tools.Util.Log
{
    /// <summary>
    /// 描   述  ：                          
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 
    /// <summary>
    /// 事件日志类
    /// </summary>
    public class EventLog
    {
        static public bool Debug = true;
        static public bool isServiceName = false;
        private StreamWriter _logWriter = null;

        /// <summary>
        /// 日志根目录:默认为当前程序目录
        /// </summary>
        public static string _logRootPath = AppDomain.CurrentDomain.BaseDirectory;

        public EventLog()
        {
            _logRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logRootPath)) Directory.CreateDirectory(_logRootPath);

        }

        /// <summary>
        /// 每个服务模块分开文件夹存放日志
        /// </summary>
        /// <param name="ServiceName"></param>
        public EventLog(bool aisServiceName, string ServiceName)
        {
            _logRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(_logRootPath)) Directory.CreateDirectory(_logRootPath);

            if (aisServiceName) _logRootPath = Path.Combine(_logRootPath, ServiceName);
            if (!Directory.Exists(_logRootPath)) Directory.CreateDirectory(_logRootPath);

        }

        /// <summary>
        /// 代替 默认的当前程序目录
        /// </summary>
        /// <param name="alogRootPath"></param>
        public EventLog(string alogRootPath)
        {
            _logRootPath = Path.Combine(alogRootPath, "Logs");
            if (!Directory.Exists(_logRootPath)) Directory.CreateDirectory(_logRootPath);
        }

        ~EventLog()
        {
            if (_logWriter != null)
                _logWriter.Close();
        }

        private void WriteLog(string Section, string Msg)
        {
            if (!Msg.Equals(""))
            {
                if (_logWriter != null)
                {
                    _logWriter.WriteLine(Section + Msg);
                }
            }
        }
        private void AddLog(string ModuleName, string Msg, string OtherMsg, bool IsError)
        {
            try
            {
                if (_logRootPath != null) _logWriter = File.AppendText(Path.Combine(_logRootPath, DateTime.Today.Year + DateTime.Today.Month.ToString("00") + DateTime.Today.Day.ToString("00") + DateTime.Now.Hour.ToString("00") + ".log"));

                string kind = IsError ? "错误" : "正常";
                WriteLog("类别：", kind);
                WriteLog("时间：", DateTime.Now.ToString());
                WriteLog("模块：", ModuleName);
                WriteLog("调试信息：", Msg);
                WriteLog("", OtherMsg);
                WriteLog("--", "--------------------------------------------------------------------------");
            }
            catch
            {
            }
            finally
            {
                try
                {
                    if (_logWriter != null)
                        _logWriter.Close();
                }
                catch
                {
                }
            }
        }
        public void AddException(string ModuleName, string Msg, Exception e)
        {
            string mask = "信息：{0}\n对象：{1}\n方法：{2}\n堆栈：{3}\n";
            string OtherMsg = String.Format(mask, e.Message, e.Source, e.TargetSite, e.StackTrace);
            e = e.InnerException;
            while (e != null)
            {
                OtherMsg += "\n" + String.Format(mask, e.Message, e.Source, e.TargetSite, e.StackTrace);
                e = e.InnerException;
            }
            AddLog(ModuleName, Msg, OtherMsg, true);

        }
        public void AddLog(string ModuleName, string Msg, bool IsError)
        {
            if (Debug || IsError)
            {
                AddLog(ModuleName, Msg, "", IsError);
            }
        }
        public void AddLog(string ModuleName, string Msg)
        {
            AddLog(ModuleName, Msg, false);
        }
    }
}
