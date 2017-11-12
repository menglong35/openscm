using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using Unity.Attributes;
using Timer = System.Timers.Timer;

namespace OpenSCM.ClientService.Application
{
    class PluginConfigUpdateService:ITaskConfigUpdateService,IDisposable
    {
        private const int CheckInterval = 10 * 60 * 1000;//10分钟检查一次配置更新

        /// <summary>
        /// 请求的Url
        /// </summary>
        private const string PluginConfigUpdateUrl = "{0}/PluginConfig?customerCode={1}&configVersion={2}";

        private volatile bool _cancel;

        private string _customerCode;

        private EsClientConfig _esClientConfig;

        private Timer _pluginConfigUpdateTimer;

        [Dependency]
        private ITaskDispatchServiceContainer _pluginDispatchServiceContainer { get; set; }
        private bool isRunning;

        #region IPluginConfigUpdateService 成员

        private const string LogFileName = "PluginConfigUpdateService";

        public void Start()
        {
            DebugLog("Start");
            try
            {
                Initialize();
                _pluginConfigUpdateTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                ExceptionLog("PluginConfigUpdateService Start Error", ex);
            }
        }

        public void Stop()
        {
            DebugLog("Stop");
            try
            {
                _cancel = true;
                while (isRunning)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                ExceptionLog("PluginConfigUpdateService Stop Error", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        private void LazyInitialize()
        {
            _esClientConfig = EsClientConfigUtils.ReadEsClientConfig();
            _customerCode = LicenseUtils.GetCustomerCode(_esClientConfig.LicenseOperatorServiceAddress);
        }

        private void Initialize()
        {
            MakeSureFileNormal();
            _pluginConfigUpdateTimer = new Timer();
            //初始化Timer
            _pluginConfigUpdateTimer = new Timer
            {
                Interval = CheckInterval,
                AutoReset = false,
            };
            _pluginConfigUpdateTimer.Elapsed += Excute;
            _cancel = false;
        }

        private void MakeSureFileNormal()
        {
            try
            {
                PathUtils.IssueFileNormal(PathUtils.PluginDefinitionPath);
            }
            catch (Exception ex)
            {
                ExceptionLog("MakeSureWritable", ex);
            }
            try
            {
                PathUtils.IssueFileNormal(PathUtils.PluginDefinitionPathBackup);
            }
            catch (Exception ex)
            {
                ExceptionLog("MakeSureWritable", ex);
            }
            try
            {
                PathUtils.IssueFileNormal(PathUtils.PluginDefinitionPathError);
            }
            catch (Exception ex)
            {
                ExceptionLog("MakeSureWritable", ex);
            }
        }

        private void Excute(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_cancel)
                {
                    return;
                }
                isRunning = true;
                LazyInitialize();
                //请求配置
                string fileMd5 = string.Empty;
                if (File.Exists(PathUtils.PluginDefinitionPath))
                {
                    fileMd5 = ComputeMD5IgnoreError(PathUtils.PluginDefinitionPath);
                }
                string url = string.Format(PluginConfigUpdateUrl, EsCloudUrlUtils.EsCloudApi,
                    _customerCode, fileMd5);
                DebugLog("Execute:" + url);
                HttpStatusCode statusCode;
                string newConfig;
                HttpUtils.HttpGet(url, _esClientConfig.ProxyAddress, out statusCode, out newConfig);
                //是否需要更新 
                if (string.IsNullOrEmpty(newConfig))
                {
                    DebugLog("No Need Update");
                    return;
                }
                //long timeTicks = DateTime.Now.Ticks;
                string backupFileName = PathUtils.PluginDefinitionPathBackup;
                if (File.Exists(backupFileName))
                {
                    File.Delete(backupFileName);
                }
                //备份原始配置
                if (File.Exists(PathUtils.PluginDefinitionPath))
                {
                    File.Copy(PathUtils.PluginDefinitionPath, backupFileName, true);
                }
                //写入新配置
                File.WriteAllText(PathUtils.PluginDefinitionPath, newConfig);
                //重启插件容器
                try
                {
                    _pluginDispatchServiceContainer.Restart();
                }
                catch (Exception ex)
                {
                    string wrongFileName = PathUtils.PluginDefinitionPathError;
                    ExceptionLog("PluginDispatchServiceContainer Restart Failed RollBack", ex);
                    RoolBack(backupFileName, wrongFileName);
                }
                DebugLog("Update Finished");
            }
            catch (Exception ex)
            {
                ExceptionLog("PluginConfigUpdateService Execute Wrong", ex);
            }
            finally
            {
                isRunning = false;
                if (!_cancel)
                {
                    _pluginConfigUpdateTimer.Enabled = true;
                }
            }
        }

        private void RoolBack(string backupFileName, string wrongFileName)
        {
            try
            {
                //将当前错误配置记录下来
                File.Copy(PathUtils.PluginDefinitionPath, wrongFileName, true);
                //还原之前的配置
                File.Copy(backupFileName, PathUtils.PluginDefinitionPath, true);
                if (_pluginDispatchServiceContainer.IsStarted)
                {
                    _pluginDispatchServiceContainer.Restart();
                }
                else
                {
                    _pluginDispatchServiceContainer.Start();
                }
            }
            catch (Exception ex)
            {
                ExceptionLog("PluginDispatchServiceContainer RoolBackm Error", ex);
            }
        }

       
        private static void ExceptionLog(string content, Exception ex)
        {
            FileLog.LogException(LogFileName, content, ex);
        }

        [Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            FileLog.LogOperation(LogFileName, message);
        }

        /// <summary>
        /// 计算文件MD5 忽略所有计算时发生的异常
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件MD5 如果发生异常 返回null</returns>
        private static string ComputeMD5IgnoreError(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }
            try
            {
                byte[] fileHash;
                using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        fileHash = md5.ComputeHash(fileStream);
                    }
                }
                StringBuilder fileHashStringBuilder = new StringBuilder(fileHash.Length * 2);
                for (int i = 0; i < fileHash.Length; i++)
                {
                    fileHashStringBuilder.Append(fileHash[i].ToString("x2"));
                }
                return fileHashStringBuilder.ToString();
            }
            catch
            {
                //计算MD5用于下载文件时 判断本地参考文件是否可以直接复制
                //如果文件md5 计算失败 例如文件没有读取权限 
                //则认为不需要复制参考文件 直接返回空md5
                return null;
            }
        }

        public void Dispose()
        {
            //待定
            Stop();
        }

        #endregion
    }
}
