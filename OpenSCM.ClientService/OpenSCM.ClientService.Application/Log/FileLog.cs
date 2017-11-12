using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// 将日志输出到文件中
    /// 可以每个类别的每个级别的信息记录在一个文件中
    /// 暂时分为两个级别
    /// Operation、和 Exception
    /// 例如 category = 0001 的异常 记录在  文件 Ex_0001_20160310.log 中
    /// 如果不规定类别 则使用默认类别 : All
    /// </summary>
    public static class FileLog
    {

        private const string DefaultCategory = "All";
        private const string FileName = "{0}_{1}_{2}.log";
        private const string Exception = "Ex";//异常日志文件前缀
        private const string Operation = "Op";//操作日志文件前缀
        private static readonly object WriteLock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="content"></param>
        /// <param name="ex"></param>
        public static void LogException(string category, string content, Exception ex)
        {
            var logFileName = GetLogFileName(category, Exception);
            var sb = new StringBuilder();
            sb.AppendLine(content);
            sb.AppendLine();
            while (ex != null)
            {
                sb.AppendLine(String.Format("{0} : {1}", "Message", ex.Message));
                sb.AppendLine(String.Format("{0} : {1}", "StackTrace ", ex.StackTrace));
                ex = ex.InnerException;
            }
            InnerWrite(logFileName, sb.ToString());
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="content"></param>
        /// <param name="ex"></param>
        public static void LogException(string content, Exception ex)
        {
            LogException(DefaultCategory, content, ex);
        }

        /// <summary>
        /// 记录操作
        /// </summary>
        /// <param name="category"></param>
        /// <param name="content"></param>
        public static void LogOperation(string category, string content)
        {
            var logFileName = GetLogFileName(category, Operation);
            InnerWrite(logFileName, content);
        }

        /// <summary>
        /// 记录操作
        /// </summary>
        /// <param name="content"></param>
        public static void LogOperation(string content)
        {
            LogOperation(DefaultCategory, content);
        }

        private static void InnerWrite(string fileName, string content)
        {
            try
            {
                //最好的方式是对将要写入的文件进行加锁，考虑到目前线程不多
                //所以简单粗暴一点  对全局加锁 暂时对性能没太大影响
                var sb = new StringBuilder();
                sb.AppendLine(string.Format("{0:yyyy-MM-dd HH:mm:ss ffff}:\n{1}", DateTime.Now, content));
                lock (WriteLock)
                {
                    PathUtils.IssueFilePathExist(fileName);
                    //20161111 by guodp for 写入时候 不带 三个字节的BOM
                    var utf8 = new UTF8Encoding(false);
                    File.AppendAllText(fileName, sb.ToString(), utf8);
                    if (Environment.UserInteractive)
                    {
                        var info = new FileInfo(fileName);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("---------------LogBegin---------------");
                        Console.ResetColor();
                        //上面是AppendLine 这里就不NewLine了
                        Console.Write(String.Format("LogName:{0}\n{1}", info.Name, sb.ToString()));
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("----------------LogEnd----------------");
                        Console.ResetColor();
                    }
                }
            }
            catch
            {
                //do nothing
            }
        }

        /// <summary>
        /// 日志文件名称
        /// </summary>
        /// <param name="category"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        private static string GetLogFileName(string category, string level)
        {
            var logPath = Path.Combine(PathUtils.LogBasePath, DateTime.Now.ToString("yyyyMMdd"));
            //每小时一个文件
            return Path.Combine(logPath, string.Format(FileName, level, category, DateTime.Now.ToString("yyyyMMddHH")));
        }

    }
}
