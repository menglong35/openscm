using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    class TaskDispatchServiceContainer : ITaskDispatchServiceContainer
    {
        [Dependency]
        private ILogService Log { get; set; }

        private const int TryTimes = 3;//获取操作锁的重试次数
        private const int WaitTimeOutInterval = 5000;//等待时间
        private readonly Mutex _operationMutex = new Mutex();//所有的操作都需要获取一个锁
        private readonly List<TaskDispatcherService> _pluginDispatchServices = new List<TaskDispatcherService>();

        private volatile bool _isStarted;

        #region IPluginDispatchServiceContainer 成员

        /// <summary>
        /// 加载所有插件并开始执行
        /// </summary>
        public void Start()
        {
            Log.LogOperation(LogFileName,"Start");
            GetOperationRight();
            try
            {
                //暂时没有任何多线程操作 PluginDispatchServiceContainer 的情况
                //都是在主线程中调用 PluginDispatchServiceContainer 的方法
                //暂时不做多线程考虑 后续如果需要再添加
                LoadPluginDispatchServices();
                StartPluginDispatchServices();
                _isStarted = true;
            }
            catch(Exception ex)
            {
                Log.LogException(LogFileName, "Error in Start,Roll Back",ex);
                try
                {
                    Stop();
                }
                catch (Exception)
                {
                    //throw;
                }
            }
            finally
            {
                ReleaseOperationRight();
            }
        }

        /// <summary>
        /// 停止并卸载所有插件
        /// </summary>
        public void Stop()
        {
            Log.LogOperation(LogFileName, "Stop");
            GetOperationRight();
            try
            {
                StopPluginDispatchServices();
                UnloadPluginDispatchServices();
                _isStarted = false;
            }
            finally
            {
                ReleaseOperationRight();
            }
        }

        /// <summary>
        /// 卸载然后重新加载并执行所有插件
        /// </summary>
        public void Restart()
        {
            Log.LogOperation(LogFileName, "Restart");
            if (!_isStarted)
            {
                //throw new InvalidOperationException(MsgLibrary.PluginDispatchServiceContainerAlreadyStoped);
            }
            GetOperationRight();
            try
            {
                StopPluginDispatchServices();
                UnloadPluginDispatchServices();
                _isStarted = false;
                LoadPluginDispatchServices();
                StartPluginDispatchServices();
                _isStarted = true;
            }
            finally
            {
                ReleaseOperationRight();
            }
        }

        /// <summary>
        /// 立刻生成所有插件执行产生的数据文件
        /// </summary>
        public void PrepareDataFiles()
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("任务调度服务容器未启动");
            }
            GetOperationRight();
            try
            {
                foreach (var pluginDispatchService in _pluginDispatchServices)
                {
                    pluginDispatchService.PrepareDataFile();
                }
            }
            finally
            {
                ReleaseOperationRight();
            }
        }

        #endregion

        /// <summary>
        /// 获取操作的锁
        /// </summary>
        private void GetOperationRight()
        {
            var tryAlready = 0;
            while (!_operationMutex.WaitOne(++tryAlready * WaitTimeOutInterval))
            {
                if (tryAlready == TryTimes)
                {
                    throw new TimeoutException("获取操作锁时间超时");
                }
            }
        }

        //释放锁
        private void ReleaseOperationRight()
        {
            _operationMutex.ReleaseMutex();
        }

        /// <summary>
        /// 加载
        /// </summary>
        private void LoadPluginDispatchServices()
        {
            PluginDefinition pluginDefinition;
            try
            {
                pluginDefinition = ReadPluginDefinitions();
            }
            catch (Exception ex)
            {
                ExceptionLog("LoadPluginDefinition Failed", ex);
                DebugLog("LoadPluginDefinition Failed,Create Empty Config");
                //如果发生异常,创建空的内容
                pluginDefinition = new PluginDefinition();
                File.WriteAllText(PathUtils.PluginDefinitionPath, SerializationUtils.XmlSerialize(pluginDefinition));
            }
            var esClientConfig = EsClientConfigUtils.ReadEsClientConfig();
            if (!esClientConfig.CustomerAgreementState.Registered)
            {
                DebugLog("Not Registered,Start None Plugin");
                return;
            }
            var serverRoles = esClientConfig.ServerRoles;
            //string customerCode = LicenseUtils.GetCustomerCode(esClientConfig.LicenseOperatorServiceAddress);
            var hostName = Dns.GetHostName();
            foreach (var pluginConfig in pluginDefinition.PluginConfigs)
            {
                //插件启用的服务器角色中 有当前服务器角色(之一) 启用
                if (!CheckEnableByServerRole(serverRoles, pluginConfig.EnableServerRoles))
                {
                    DebugLog(string.Format("Server Role Not Match:[{0}][{1}][{2}]",
                        pluginConfig.TypeKey, string.Join(",", serverRoles.ToArray()),
                        string.Join(",", pluginConfig.EnableServerRoles.ToArray())));
                    continue;
                }
                if (!CheckEnableByAgreement(esClientConfig.CustomerAgreementState, pluginConfig.PluginType))
                {
                    DebugLog(string.Format("Agreement Not Match:[{0}][{1}][{2}][{3}][{4}]",
                        pluginConfig.TypeKey, esClientConfig.CustomerAgreementState.Registered,
                        esClientConfig.CustomerAgreementState.AgreeToUxImproving,
                        esClientConfig.CustomerAgreementState.AgreeToE10Examination,
                        pluginConfig.PluginType));
                    continue;
                }
                var context = new PluginExecuteContext(pluginConfig, serverRoles, null,
                    hostName, esClientConfig.LicenseOperatorServiceAddress);
                var pluginDispatchService = new PluginDispatchService(context);
                //注入服务环境
                Connection(pluginDispatchService, ResourceServiceProvider, ServiceCallContext);
                DebugLog("Add Plugin:" + pluginConfig.TypeKey);
                _pluginDispatchServices.Add(pluginDispatchService);
            }
        }

        internal static PluginDefinition ReadPluginDefinitions()
        {
            var pluginDefinition = SerializationUtils.XmlDeserialize<PluginDefinition>(File.ReadAllText(PathUtils.PluginDefinitionPath));
            pluginDefinition.PluginConfigs.Add(ServerStatusPlugin.GetPluginConfig());
            pluginDefinition.PluginConfigs.Add(EsClientLogPlugin.GetPluginConfig());
            return pluginDefinition;
        }

        private bool CheckEnableByAgreement(CustomerAgreementState customerAgreementState, PluginType pluginType)
        {
            switch (pluginType)
            {
                case PluginType.Internal:
                case PluginType.Debug:
                    return true;
                case PluginType.Base:
                case PluginType.Customer:
                case PluginType.Warning:
                    return customerAgreementState.Actived;
                case PluginType.E10Examination:
                    return customerAgreementState.AgreeToE10Examination & customerAgreementState.Actived;   //20170606 modi by zhanghje(01436) for 预警改善_服务器预警设置  add  & customerAgreementState.Actived
                case PluginType.UxImproving:
                    return customerAgreementState.AgreeToUxImproving & customerAgreementState.Actived;   //20170606 modi by zhanghje(01436) for 预警改善_服务器预警设置  add  & customerAgreementState.Actived
            }
            return false;
        }

        /// <summary>
        /// 开始
        /// </summary>
        private void StartPluginDispatchServices()
        {
            foreach (var pluginDispatchService in _pluginDispatchServices)
            {
                pluginDispatchService.Start();
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        private void StopPluginDispatchServices()
        {
            foreach (var pluginDispatchService in _pluginDispatchServices)
            {
                pluginDispatchService.Stop();
            }
        }

        /// <summary>
        /// 卸载
        /// </summary>
        private void UnloadPluginDispatchServices()
        {
            foreach (var pluginDispatchService in _pluginDispatchServices)
            {
                Disconnection(pluginDispatchService);
            }
            _pluginDispatchServices.Clear();
        }

        //检查当前服务器角色 和插件配置中运行插件的服务器角色 判断插件是否运行
        private static bool CheckEnableByServerRole(Collection<string> currentRoles, IEnumerable<string> enableRoles)
        {
            foreach (var enableRole in enableRoles)
            {
                var enableRoleUpper = enableRole.ToUpper();
                if (enableRoleUpper.Equals(ServerRole.All))
                {
                    return true;
                }
                foreach (var currentRole in currentRoles)
                {
                    var currentRoleUpper = currentRole.ToUpper();
                    if (currentRoleUpper.Equals(ServerRole.All) || currentRoleUpper.Equals(enableRoleUpper))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #region IPluginDispatchServiceContainer 成员

        private const string LogFileName = "TaskDispatchServiceContainer";

        /// <summary>
        /// 是否启动
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return _isStarted;
            }
        }

        #endregion
    }
}
