using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using Unity.Attributes;
using Timer = System.Timers.Timer;

namespace OpenSCM.ClientService.Application
{
    class DispatcherService :IDispatcherService,IDisposable
    {
        //担心如下顺序执行的情况，因此设置 volatile:
        //1. 主线程设置 cancel = true
        //2. 工作线程获取到的 cancel 是false
        //3. 主线程获取 _running = false
        //4. 主线程认为可以退出 Stop 结束
        //5.工作线程设置 _running= true
        private volatile bool _cancel;
        private Timer _dispatcherTimer;

        [Dependency]
        private IDataUploadService _dataUploadService { get; set; }

        [Dependency]
        private ITaskDispatchServiceContainer pluginDispatchServiceContainer { get; set; }

        //不需要 volatile
        private bool _running;

        #region IDispatcherService Members

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            //初始化变量
            Initialize();
            //启动定时器
            _dispatcherTimer.Enabled = true;
        }

        public void Stop()
        {
            _cancel = true;
            //等待所有运行的操作完成 才可以进行下面的操作
            while (_running)
            {
                Thread.Sleep(100);
            }
        }

        #endregion

        private void Excute(object sender, ElapsedEventArgs e)
        {
            if (_cancel)
            {
                return;
            }
            Timer t = (Timer)sender;
            try
            {
                _running = true;
                DispatcherExcute();
            }
            catch (Exception ex)
            {
                FileLog.LogException("DispatcherService", "派班中心执行异常", ex);
            }
            finally
            {
                //设置标志位
                _running = false;
                if (!_cancel)
                {
                    //如果取消了 则不需要重新启动定时器
                    t.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing">是否已经dispose过</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            //派班中心一个小时执行一次
            _dispatcherTimer = new Timer
            {
                Interval = 3600 * 1000
            };
            _dispatcherTimer.Elapsed += Excute;
            _dispatcherTimer.AutoReset = false;
            //设置取消标志位
            _cancel = false;
        }

        /// <summary>
        /// 获取插件列表，循环处理
        /// </summary>
        internal void DispatcherExcute()
        {
            DebugLog("Begin DispatcherExcute");
            //准备数据
            if (pluginDispatchServiceContainer !=null)
            {
                try
                {
                    pluginDispatchServiceContainer.PrepareDataFiles();
                }
                catch (Exception ex)
                {
                    ExceptionLog("DispatcherExcute.pluginDispatchServiceContainer.PrepareDataFiles()", ex);
                }
            }

            //上传数据
            
            if (_dataUploadService!= null)
            {
                try
                {
                    _dataUploadService.RequestUpload();
                }
                catch (Exception ex)
                {
                    ExceptionLog("DispatcherExcute.dataUploadService.RequestUpload()", ex);
                }
            }

            //广播一次配置
            //IEsMasterConfigManager esMasterConfigService;
            //if (TryGetService(out esMasterConfigService))
            //{
            //    try
            //    {
            //        esMasterConfigService.RequestBroadcast();
            //    }
            //    catch (Exception ex)
            //    {
            //        ExceptionLog("DispatcherExcute.esMasterConfigService.Broadcast()", ex);
            //    }
            //}
            //DebugLog("End DispatcherExcute");
        }

        private static void ExceptionLog(string content, Exception ex)
        {
            FileLog.LogException("DispatcherService", content, ex);
        }

        [Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            FileLog.LogOperation("DispatcherService", message);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
