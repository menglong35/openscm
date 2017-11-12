using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// </summary>
    public static class PluginExecuteHelper
    {

        static PluginExecuteHelper()
        {
            Initialize();
        }

        private static readonly List<string> TypeKeys = new List<string>();

        private static string _lastDate;

        [Dependency]
        private static ILogService Log { get; set; }

        private static void TrySaveAndResetAll(string currentDate)
        {
            if (string.IsNullOrEmpty(_lastDate))
            {
                _lastDate = currentDate;
                return;
            }
            //如果天数发生变化 则需要保存
            if (!string.Equals(_lastDate, currentDate))
            {
                var keys = TypeKeys.ToArray();
                foreach (var typeKey in keys)
                {
                    var newsummary = new ExecuteSummary
                    {
                        TypeKey = typeKey,
                        Load = 0
                    };
                    SaveSummary(newsummary, currentDate);
                    _lastDate = currentDate;
                }
            }
        }

        internal static void Register(string typeKey)
        {
            try
            {
                DebugLog("Register:" + typeKey);
                lock (TypeKeys)
                {
                    if (TypeKeys.Contains(typeKey))
                    {
                        return;
                    }
                    TypeKeys.Add(typeKey);
                }
                var date = CurrentDate;
                var summary = ReadSummary(typeKey, date);
                summary.Load++;
                SaveSummary(summary, date);
            }
            catch (Exception ex)
            {
                ExceptionLog("Register:" + typeKey, ex);
            }
        }

        internal static PluginExecuteScope GetExecuteScope(string typeKey)
        {
            return new PluginExecuteScope(typeKey);
        }

        private static void EndScope(ExecuteDetail detail)
        {
            var currentDate = CurrentDate;
            //write record
            WriteRecord(detail);
            //save detail
            SaveDetail(detail, currentDate);
            //update summary
            UpdateSummary(detail, currentDate);
        }

        private static void Initialize()
        {
            try
            {
                PathUtils.IssueDirectoryPathExist(PathUtils.PluginExecuteRecordPath);
            }
            catch (Exception ex)
            {
                ExceptionLog("PluginExecuteHelper Initialize Failed", ex);
            }
        }

        internal static string CurrentDate
        {
            get
            {
                return DateTime.Now.ToString("yyyyMMdd");
            }
        }

        internal static string GetDictionary(string date)
        {
            var path = Path.Combine(PathUtils.PluginExecuteRecordPath, date);
            PathUtils.IssueDirectoryPathExist(path);
            return path;
        }

        /// <summary>
        /// </summary>
        /// <param name="typeKey"></param>
        /// <param name="record"></param>
        internal static bool TryReadRecord(string typeKey, out ExecuteRecord record)
        {
            record = null;
            var fileName = GetRecordFileName(typeKey);
            try
            {
                if (File.Exists(fileName))
                {
                    record = SerializationUtils.XmlDeserializeFromFile<ExecuteRecord>(fileName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(string.Format("TryReadRecord PluginExecuteRecord {0}", typeKey), ex);
            }
            //如果没有找到执行记录
            //则将创建执行记录设置为当前时间
            ForceCreateRecord(typeKey);
            return false;
        }

        private static void ForceCreateRecord(string typeKey)
        {
            var record = new ExecuteRecord
            {
                TypeKey = typeKey,
                LastExecuteTime = DateTime.Now
            };
            WriteRecordInteranl(record);
        }

        private static string GetRecordFileName(string typeKey)
        {
            const string recordFileName = "{0}.record";
            return Path.Combine(PathUtils.PluginExecuteRecordPath, string.Format(recordFileName, typeKey));
        }

        private static string GetSummaryFileName(string typeKey, string date)
        {
            var directory = Path.Combine(PathUtils.PluginExecuteRecordPath, date);
            PathUtils.IssueDirectoryPathExist(directory);
            const string summaryFileName = "{0}.{1}.summary";
            return Path.Combine(directory, string.Format(summaryFileName, typeKey, date));
        }

        private static string GetDetailFileName(string typeKey, string date)
        {
            var directory = Path.Combine(PathUtils.PluginExecuteRecordPath, date);
            PathUtils.IssueDirectoryPathExist(directory);
            const string detailFileName = "{0}.{1}.detail";
            return Path.Combine(directory, string.Format(detailFileName, typeKey, date));
        }

        private static ExecuteSummary ReadSummary(string typeKey, string date)
        {
            var summaryFullName = GetSummaryFileName(typeKey, date);
            if (File.Exists(summaryFullName))
            {
                return SerializationUtils.XmlDeserializeFromFile<ExecuteSummary>(summaryFullName);
            }
            return new ExecuteSummary
            {
                TypeKey = typeKey
            };
        }

        private static void SaveSummary(ExecuteSummary summary, string date)
        {
            var summaryFullName = GetSummaryFileName(summary.TypeKey, date);
            File.WriteAllText(summaryFullName, SerializationUtils.XmlSerialize(summary));
        }

        private static void UpdateSummary(ExecuteDetail detail, string date)
        {
            var typeKey = detail.TypeKey;
            TrySaveAndResetAll(date);
            var summary = ReadSummary(typeKey, date);
            summary.TotalExecute++;
            summary.TotalElapsed += detail.Elapsed;
            summary.TotalSuccess += detail.Success;
            if (detail.Successed)
            {
                summary.LastSuccessExecute = detail.End;
            }
            summary.TotalFailed += detail.Failed;
            summary.TotalMissed += detail.Missed;
            summary.TotalCancled += detail.Canceled;
            SaveSummary(summary, date);
        }

        private static void WriteRecord(ExecuteDetail detail)
        {
            var record = new ExecuteRecord
            {
                TypeKey = detail.TypeKey,
                LastExecuteTime = detail.End
            };
            WriteRecordInteranl(record);
        }

        private static void SaveDetail(ExecuteDetail detail, string date)
        {
            var typeKey = detail.TypeKey;
            var filePath = GetDetailFileName(typeKey, date);
            File.AppendAllText(filePath, detail.ToString());
            File.AppendAllText(filePath, Environment.NewLine);
        }

        private static void WriteRecordInteranl(ExecuteRecord record)
        {
            try
            {
                var fileName = GetRecordFileName(record.TypeKey);
                File.WriteAllText(fileName, SerializationUtils.XmlSerialize(record));
            }
            catch (Exception ex)
            {
                ExceptionLog(string.Format("WriteRecord {0}", record.TypeKey), ex);
            }
        }


        private static void DebugLog(string content)
        {
            Log.DebugLogEx("PluginExecuteHelper", content);
        }

        private static void ExceptionLog(string content, Exception ex)
        {
            Log.LogException("PluginExecuteHelper", content, ex);
        }

        #region Nested type: PluginExecuteScope

        /// <summary>
        /// </summary>
        internal class PluginExecuteScope : IDisposable
        {

            private readonly ExecuteDetail _executeDetail;

            public PluginExecuteScope(string typeKey)
            {
                _executeDetail = new ExecuteDetail(typeKey)
                {
                    Begin = DateTime.Now
                };
            }

            #region IDisposable 成员

            public void Dispose()
            {
                _executeDetail.End = DateTime.Now;
                try
                {
                    EndScope(_executeDetail);
                }
                catch (Exception ex)
                {
                    ExceptionLog(string.Format("Dispose Scope Failed {0}", _executeDetail.TypeKey), ex);
                }
            }

            #endregion

            internal void Fail()
            {
                _executeDetail.Failed = 1;
            }

            internal void Cancel()
            {
                _executeDetail.Canceled = 1;
            }

            internal void Miss()
            {
                _executeDetail.Missed = 1;
            }

            internal void Success()
            {
                _executeDetail.Success = 1;
            }

        }

        #endregion
    }

    /// <summary>
    ///     执行汇总
    /// </summary>
    public class ExecuteSummary
    {

        /// <summary>
        /// </summary>
        public string TypeKey
        {
            get;
            set;
        }

        /// <summary>
        ///     总共执行次数
        /// </summary>
        public int TotalExecute
        {
            get;
            set;
        }

        /// <summary>
        ///     总计执行时间(只有成功的才有时间)
        /// </summary>
        public double TotalElapsed
        {
            get;
            set;
        }

        /// <summary>
        ///     成功次数
        /// </summary>
        public int TotalSuccess
        {
            get;
            set;
        }

        /// <summary>
        ///     失败次数
        /// </summary>
        public int TotalFailed
        {
            get;
            set;
        }

        /// <summary>
        ///     取消执行的次数
        /// </summary>
        public int TotalCancled
        {
            get;
            set;
        }

        /// <summary>
        ///     错过执行次数
        /// </summary>
        public int TotalMissed
        {
            get;
            set;
        }

        /// <summary>
        ///     加载次数
        /// </summary>
        public int Load
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public DateTime LastSuccessExecute
        {
            get;
            set;
        }

    }

    /// <summary>
    ///     执行明细
    /// </summary>
    public class ExecuteDetail
    {

        /// <summary>
        /// </summary>
        /// <param name="typeKey"></param>
        public ExecuteDetail(string typeKey)
        {
            TypeKey = typeKey;
        }

        /// <summary>
        /// </summary>
        public string TypeKey
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public DateTime Begin
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public int Success
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public int Failed
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public int Canceled
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public int Missed
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public DateTime End
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public double Elapsed
        {
            get
            {
                if (Success == 1)
                {
                    return (End - Begin).TotalMilliseconds;
                }
                return 0;
            }
        }

        private string Result
        {
            get
            {
                if (Success == 1)
                {
                    return "Success";
                }
                if (Failed == 1)
                {
                    return "Failed";
                }
                if (Canceled == 1)
                {
                    return "Canceled";
                }
                if (Missed == 1)
                {
                    return "Missed";
                }
                return "Unknow";
            }
        }

        /// <summary>
        /// </summary>
        public bool Successed
        {
            get
            {
                return Success == 1;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}]:[{1}],[{2}],[{3}],[{4}]", TypeKey, Begin, End, Result, Elapsed);
        }

    }

    /// <summary>
    ///     执行记录
    /// </summary>
    public class ExecuteRecord
    {

        /// <summary>
        /// </summary>
        public string TypeKey
        {
            get;
            set;
        }

        /// <summary>
        /// </summary>
        public DateTime LastExecuteTime
        {
            get;
            set;
        }

    }
}
