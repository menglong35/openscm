using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    interface ITaskConfigUpdateService
    {
        /// <summary>
        /// 启动服务
        /// </summary>
        void Start();

        /// <summary>
        /// 关闭服务 
        /// </summary>
        void Stop();

        /// <summary>
        /// 重启插件配置更新服务
        /// </summary>
        void Restart();
    }
}
