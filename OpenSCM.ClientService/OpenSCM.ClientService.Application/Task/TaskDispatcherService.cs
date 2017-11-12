using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    class TaskDispatcherService : IDisposable
    {
        private const string SplitString = "4dec3227-4ad5-48b8-b766-49b4509138cc";

        private readonly PluginConfig _pluginConfig;
        private readonly PluginExecuteContext _pluginExecuteContext;

        private volatile bool _cancel;
        private IDataUploadService _dataUploadService;
        private bool _executing;
        private string _fileName;
        private Stream _fileStream;
        private IPlugin _plugin;
        private Timer _pluginTimer;
        private int _runningCount; // 正在运行的操作数 解决多线程问题

        /// <summary>
        ///     插件调度服务
        /// </summary>
        /// <param name="pluginExecuteContext">插件执行的上下文参数</param>
        public TaskDispatcherService(PluginExecuteContext pluginExecuteContext)
        {
            _pluginExecuteContext = pluginExecuteContext;
            _pluginConfig = pluginExecuteContext.PluginConfig;
        }

        /// <summary>
        ///     数据上传服务
        /// </summary>
        [Dependency]
        private IDataUploadService DataUploadService
        {
            get; set;
        }
        [Dependency]
        private ILogService Log { get; set; }



        /// <summary>
        ///     开始调度
        /// </summary>
        public void Start()
        {
            DebugLogEx("Start");
            try
            {
                Initialize();
                StartTimer();
            }
            catch (Exception ex)
            {
                ExceptionLog(
                    string.Format("任务调度服务启动异常{0}", _pluginConfig.TypeKey),
                    ex);
            }
        }

        /// <summary>
        ///     立刻运行一次 返回收集的数据(string)
        /// </summary>
        public string RunOnceSync()
        {
            CreatePlugin();
            var executeResult = _plugin.Execute();
            if (!executeResult.NeedUpload
                || string.IsNullOrEmpty(executeResult.DataContent))
            {
                return string.Empty;
            }
            var jsonContent = PackageData(executeResult.DataContent);
            return jsonContent;
        }

        private void StartTimer()
        {
            //初始化Timer
            _pluginTimer = new Timer(Excute, null, -1, -1);
            //设置标志位
            _cancel = false;
            ExecuteRecord record;
            int executeDelaySecond;
            DebugLogEx(string.Format("StartTimer,PluginConfig.ResetTimer {0}", _pluginConfig.ResetTimer));
            //如果 ResetTimerWhenStartUp 为false 并且可以获取到上次执行时间
            //则根据上次执行时间推算得出开始执行的延迟时间
            var findRecod = PluginExecuteHelper.TryReadRecord(_pluginConfig.TypeKey, out record);
            if (!_pluginConfig.ResetTimer
                && findRecod
                && _pluginConfig.CollectionInterval > 60)
            {
                var now = DateTime.Now;
                var spanSecond = (int)(now - record.LastExecuteTime).TotalSeconds;
                executeDelaySecond = spanSecond > _pluginConfig.CollectionInterval
                    ? 0
                    : _pluginConfig.CollectionInterval - spanSecond;
                DebugLogEx(
                    string.Format(
                        "StartTimer,Use ExecuteDelay In ExecuteRecord: Now {0},LastExecuteTime {1}",
                        now, record.LastExecuteTime));
            }
            else
            {
                //如果 ResetTimerWhenStartUp 为true
                //或者 获取不到 LastExecuteTime 
                //或者 CollectionInterval 小等于 60秒
                //则使用 ExecuteDelay 作为延迟
                //如果没有 使用插件中定义的延迟时间
                executeDelaySecond = _pluginConfig.ExecuteDelay;
                DebugLogEx("StartTimer,Use PluginConfig.ExecuteDelay");
            }
            DebugLogEx(
                string.Format(
                    "StartTimer,Execute Delay Time:{0} ,Interval= {1}", executeDelaySecond,
                    _pluginConfig.CollectionInterval));
            //启动执行
            var dueTime = new TimeSpan(executeDelaySecond * TimeSpan.TicksPerSecond);
            var period = new TimeSpan(_pluginConfig.CollectionInterval * TimeSpan.TicksPerSecond);
            _pluginTimer.Change(dueTime, period);
            //20161123 by guodp for 注册插件 记录插件执行记录
            PluginExecuteHelper.Register(_pluginConfig.TypeKey);
        }

        /// <summary>
        ///     停止运行
        /// </summary>
        public void Stop()
        {
            DebugLogEx("Stop");
            try
            {
                _cancel = true;
                //计时器停止
                if (_pluginTimer != null)
                {
                    _pluginTimer.Change(-1, -1);
                }
                //等待所有运行的操作完成 才可以进行下面的操作
                while (_runningCount > 0)
                {
                    Thread.Sleep(100);
                }
                if (_fileStream != null)
                {
                    lock (this)
                    {
                        _fileStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog(
                    string.Format("PluginDispacherService Stop Failed (TypeKey={0})", _pluginConfig.TypeKey),
                    ex);
            }
        }

        /// <summary>
        ///     准备数据文件:将收集的数据生成数据文件放到数据上传区
        /// </summary>
        public void PrepareDataFile()
        {
            try
            {
                if (_cancel)
                {
                    //如果取消 就不需要准备数据文件了
                    return;
                }
                Interlocked.Increment(ref _runningCount);
                var oldFileName = CutDataFile();
                MoveDataFile(oldFileName);
            }
            catch (Exception ex)
            {
                ExceptionLog(
                    string.Format(
                        "准备数据文件异常[{0}]",
                        _pluginConfig.TypeKey), ex);
            }
            finally
            {
                Interlocked.Decrement(ref _runningCount);
            }
        }

        /// <summary>
        ///     数据主方法
        /// </summary>
        private void Excute(object sender)
        {
            DebugLogEx(string.Format("Excute"));
            using (
                var scope =
                    PluginExecuteHelper.GetExecuteScope(_pluginConfig.TypeKey))
            {
                try
                {
                    if (_cancel)
                    {
                        DebugLogEx(string.Format("Excute Canceled"));
                        scope.Cancel();
                        return;
                    }
                    //检查是否可以执行
                    if (!CheckExecutable())
                    {
                        scope.Miss();
                        return;
                    }
                    try
                    {
                        Interlocked.Increment(ref _runningCount);
                        _executing = true;
                        InnerExcute();
                        scope.Success();
                    }
                    catch
                    {
                        scope.Fail();
                        throw;
                    }
                    finally
                    {
                        _executing = false;
                        Interlocked.Decrement(ref _runningCount);
                    }
                }
                catch (Exception ex)
                {
                    ExceptionLog(string.Format("Excute Error"), ex);
                }
            }
        }

        private bool CheckExecutable()
        {
            switch (_pluginConfig.ExecuteType)
            {
                case ExecuteType.Run:
                    return true;
                case ExecuteType.Miss:
                    if (!_executing)
                    {
                        return true;
                    }
                    DebugLogEx(
                        string.Format(
                            "ExecuteType.Miss : Other Thread Running," +
                            "Not Executable,RunningCount:{0} ",
                            _runningCount));
                    break;
                case ExecuteType.Wait:
                    for (var waitTimes = 0; waitTimes < 3600; waitTimes++)
                    {
                        if (!_executing)
                        {
                            return true;
                        }
                        Thread.Sleep(1000);
                    }
                    DebugLogEx(
                        string.Format(
                            "ExecuteType.Wait : Other Thread Running , " +
                            "Wait Too Long ,Not Executable,RunningCount:{0} "
                            , _runningCount));
                    break;
            }
            return false;
        }

        /// <summary>
        ///     初始化 (包括初始化收集数据文件)
        /// </summary>
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            CreatePlugin();
            CreateFile();
            _initialized = true;
        }

        //实例化插件
        private void CreatePlugin()
        {
            _plugin = (IPlugin)Activator.CreateInstance(Type.GetType(_pluginConfig.AssemblyQualifiedName));
            _plugin.PluginExecuteContext = _pluginExecuteContext;
            ConnectionAddin(this, _plugin);
        }

        private void CreateFile()
        {
            //准备数据收集文件
            var fileExist = false;
            _fileName = _pluginConfig.UploadType + "_" + _pluginConfig.TypeKey + "_";
            var folder = new DirectoryInfo(_pluginExecuteContext.CollectedDataPath);
            //2016-03-09 by guodp for 最好判断一下目录是否存在
            if (folder.Exists)
            {
                foreach (var file in folder.GetFiles(_fileName + "*.txt"))
                {
                    //如果存在正在写的文件 并且是最近15天的 直接使用
                    if (file.Name.EndsWith("_U.txt"))
                    {
                        if ((file.LastWriteTime.AddDays(15) >= DateTime.Now))
                        {
                            _fileName = file.Name;
                            _fileStream =
                                new FileStream(
                                    Path.Combine(_pluginExecuteContext.CollectedDataPath, _fileName),
                                    FileMode.Append,
                                    FileAccess.Write);
                            fileExist = true;
                            break;
                        }
                        //删除文件
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {

                        }
                    }
                }
            }
            if (!fileExist)
            {
                _fileName = _fileName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + "U.txt";
                _fileStream = new FileStream(
                    Path.Combine(_pluginExecuteContext.CollectedDataPath, _fileName), FileMode.Create,
                    FileAccess.ReadWrite);
            }
        }

        private bool _initialized;

        /// <summary>
        ///     数据收集具体逻辑
        /// </summary>
        private void InnerExcute()
        {
            //收集
            //数据收集 生成上传格式
            var executeResult = _plugin.Execute();
            if (!executeResult.NeedUpload)
            {
                return;
            }
            var jsonContent = PackageData(executeResult.DataContent);
            //追加到数据文件
            AppendDataFile(jsonContent);
            //即时上传 UploadType.I 需要即时上传
            if (_pluginConfig.UploadType == UploadType.I)
            {
                PrepareDataFile();
                DataUploadService.RequestUpload();
            }
        }

        /// <summary>
        ///     打包 生成上传结构  如果需要加密 可以在这里进行
        /// </summary>
        /// <param name="dataContent">需要上传的数据</param>
        /// <returns>打包后的数据</returns>
        private string PackageData(string dataContent)
        {
            var uploadData = new UploadData
            {
                CustomerCode = _pluginExecuteContext.CustomerCode,
                ServerName = _pluginExecuteContext.ServerName,
                ServerRoles = _pluginExecuteContext.CurrentServerRoles,
                CollectedTime = DateTime.Now,
                DataContent = dataContent
            };
            var jsonContent = SerializationUtils.JsonSerialization(uploadData);
            return jsonContent;
        }

        /// <summary>
        ///     将数据追加到数据文件中
        /// </summary>
        /// <param name="jsonContent"></param>
        private void AppendDataFile(string jsonContent)
        {
            lock (this)
            {
                jsonContent = jsonContent + SplitString;
                var myByte = Encoding.UTF8.GetBytes(jsonContent);
                _fileStream.Write(myByte, 0, myByte.Length);
            }
        }

        /// <summary>
        ///     断开正在写入的文件，从新生成新的文件
        /// </summary>
        /// <returns></returns>
        private string CutDataFile()
        {
            //将正在写入的文件(后缀 _U) 停止写入
            //并将后缀名称修改为 _S.
            //即正在写入的文件 I_TypeKey_201603091625_U.txt  ---->  I_TypeKey_201603091625_S.txt
            //然后 生成一个新的 可写入数据的文件
            //随后 被截断的文件不再写入 可以进行上传
            var finishedFileName = _fileName;
            lock (this)
            {
                //如果没有数据 不需要截取文件
                if (_fileStream.Length > 0)
                {
                    DebugLogEx(String.Format("PreparingDataFile:{0}", _fileName));
                    _fileStream.Close();
                    finishedFileName = _fileName.Replace("_U.", "_S.");
                    var usingfilePath = Path.Combine(_pluginExecuteContext.CollectedDataPath, _fileName);
                    var finishedFilePath = Path.Combine(_pluginExecuteContext.CollectedDataPath, finishedFileName);
                    //使用移动方式修改文件名
                    File.Move(usingfilePath, finishedFilePath);
                    //创建新的文件
                    _fileName = _pluginConfig.UploadType + "_" + _pluginConfig.TypeKey + "_" +
                                DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + "U.txt";
                    var filePath = Path.Combine(_pluginExecuteContext.CollectedDataPath, _fileName);
                    _fileStream = new FileStream(
                        filePath, FileMode.Create,
                        FileAccess.ReadWrite);
                }
            }
            return finishedFileName;
        }

        /// <summary>
        ///     从数据收集文件夹移动到待上传文件夹
        /// </summary>
        /// <param name="fileName"></param>
        private void MoveDataFile(string fileName)
        {
            //将文件I_TypeKey_201603091625_S.txt(写入完成的文件) 移动成 I_TypeKey_201603091625_C.txt(正在Copy的文件)
            //将 I_TypeKey_201603091625_C.txt  复制到 上传目录 
            //删除 I_TypeKey_201603091625_C.txt(正在复制的文件)
            //完成复制
            if (fileName.EndsWith("_S.txt"))
            {
                var copyingFileName = fileName.Replace("_S.", "_C.");
                var waitingFileName = fileName.Replace("_S.", "_W.");
                var sourceFilePath = Path.Combine(_pluginExecuteContext.CollectedDataPath, fileName);
                var copyingFilePath = Path.Combine(_pluginExecuteContext.CollectedDataPath, copyingFileName);
                var waitingFilePath = Path.Combine(_pluginExecuteContext.SendDataPath, waitingFileName);
                File.Move(sourceFilePath, copyingFilePath);
                File.Copy(copyingFilePath, waitingFilePath);
                File.Delete(copyingFilePath);
                //DebugLog(String.Format("PreparedDataFile:{0}", waitingFilePath));
            }
        }

        /// <summary>
        ///     线程名称
        /// </summary>
        /// <returns></returns>
        private string GetThreadName()
        {
            return String.Format("DataCollection_{0}", _pluginConfig.TypeKey);
        }

        public void Dispose()
        {
            Stop();
        }

        private void ExceptionLog(string content, Exception ex)
        {
            Log.LogException(GetThreadName(), content, ex);
        }

        [Conditional("DEBUG")]
        private void DebugLogEx(string message)
        {
            Log.DebugLogEx(GetThreadName(), message);
        }
    }
}
