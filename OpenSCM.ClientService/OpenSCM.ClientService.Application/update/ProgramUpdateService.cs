using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// 自动更新服务
    /// </summary>
    internal class ProgramUpdateService :  IProgramUpdateService
    {

        private const string ClientUpdateAddress = "{0}/OpenSCMClient";
        private const string LogFileName = "Update";

        [Dependency]
        private IClientConfigService configService { get; set; }
        [Dependency]
        private ILogService log { get; set; }

        private ProgramUpdater _programUpdater;

        #region IProgramUpdateService Members

        /// <summary>
        /// 开始
        /// </summary>
        /// <exception cref="Exception">Updater开始执行可能抛出超时等异常</exception>
        public void Start()
        {
            string updateAddress = string.Format(ClientUpdateAddress, OpenSCMUpdateUrl);
            string proxyAddress = configService.Config.ProxyAddress;
            _programUpdater = new ProgramUpdater(updateAddress, proxyAddress);
            _programUpdater.UpdateCompleted += _programUpdater_UpdateCompleted;
            _programUpdater.UpdateAsync();
        }

        public string OpenSCMUpdateUrl
        {
            get
            {
                //请求配置
                string url = string.Format(UrlConstant.OpenSCMUpdateApiUrl, "");

                HttpStatusCode statusCode;
                string newUlr;
                string proxyAddress = configService.Config.ProxyAddress;
                HttpUtils.HttpGet(url, proxyAddress, out statusCode, out newUlr);
                //是否需要更新 
                if (string.IsNullOrEmpty(newUlr))
                {
                    return UrlConstant.OpenSCMDefaultUpdateUrl;
                }
                //此处待完善，考虑加密
                return newUlr;
            }
        }
        /// <summary>
        /// Stop 时会取消下载(如果正在下载的话),Stop过程可能需要等待一段时间 
        /// </summary>
        ///<exception cref="Exception">Updater取消可能抛出超时等异常</exception>
        public void Stop()
        {
            if (_programUpdater != null)
            {
                _programUpdater.Cancel();
                _programUpdater.UpdateCompleted -= _programUpdater_UpdateCompleted;
                _programUpdater = null;
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

        #endregion

        private void _programUpdater_UpdateCompleted(object sender, UpdateEventArgs e)
        {
            try
            {
                if (e.HasError)
                {
                    log.LogException(LogFileName,"Update Error", e.Error);
                }
                else
                {
                    string downloadTime = string.Format("Start {0},End {1}", e.Start, e.End);
                    log.LogOperation(downloadTime);
                    if (e.DownloadResult != null && e.DownloadResult.HasLastVersionAndDownloaded)
                    {
                        log.LogOperation(LogFileName, "RunningVersion:" + e.DownloadResult.RunningVersion);
                        log.LogOperation(LogFileName, "DownloadedVersion:" + e.DownloadResult.DownloadedVersion);
                        log.LogOperation(LogFileName, "SetupExePath:" + e.SetupExePath);
                    }
                    else
                    {
                        log.LogOperation(LogFileName, "No Update");
                    }
                }
            }
            catch
            {
            }
        }

    }
}
