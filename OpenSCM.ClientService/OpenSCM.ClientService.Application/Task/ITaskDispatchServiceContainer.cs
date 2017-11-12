using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    internal interface ITaskDispatchServiceContainer
    {
        /// <summary>
        /// 是否启动
        /// </summary>
        bool IsStarted
        {
            get;
        }

        /// <summary>
        /// 加载所有插件并开始执行
        /// </summary>
        /// <exception cref="TimeoutException">在设定时间内获取操作锁失败 抛出超时异常</exception>
        /// <exception cref="InvalidOperationException">如果已经处于Start状态 再次调用Start 会抛出非法操作异常</exception>
        void Start();

        /// <summary>
        /// 停止并卸载所有插件的执行
        /// </summary>
        /// <exception cref="TimeoutException">在设定时间内获取操作锁失败 抛出超时异常</exception>
        /// <exception cref="InvalidOperationException">如果没有处于Start状态 调用该方法会抛出非法操作异常</exception>
        void Stop();

        /// <summary>
        /// 卸载现有插件后重新加载并开始执行
        /// </summary>
        /// <exception cref="TimeoutException">在设定时间内获取操作锁失败 抛出超时异常</exception>
        /// <exception cref="InvalidOperationException">如果没有处于Start状态 调用该方法会抛出非法操作异常</exception>
        void Restart();

        /// <summary>
        /// 立刻生成所有插件执行产生的数据文件
        /// </summary>
        /// <exception cref="TimeoutException">在设定时间内获取操作锁失败 抛出超时异常</exception>
        /// <exception cref="InvalidOperationException">如果没有处于Start状态 调用该方法会抛出非法操作异常</exception>
        void PrepareDataFiles();
    }
}
