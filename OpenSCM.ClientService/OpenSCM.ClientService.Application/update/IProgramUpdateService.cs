using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// 应用程序更新服务 提供更新下载以及启动安装程序功能
    /// </summary>
    internal interface IProgramUpdateService
    {

        /// <summary>
        /// 启动更新服务 停止时 请调用Dispose方法
        /// </summary>
        void Start();

        /// <summary>
        /// 停止更新服务
        /// </summary>
        void Stop();

        /// <summary>
        /// 重启自动更新服务
        /// </summary>
        void Restart();

    }
}
