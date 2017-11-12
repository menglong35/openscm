using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    public interface IDispatcherService
    {
        /// <summary>
        /// 服务启动
        /// </summary>
        void Start();

        /// <summary>
        /// 服务停止
        /// </summary>
        void Stop();
    }
}
