using OpenSCM.ClientService.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    public class CSApplication
    {
        private IUServiceProvider _container;
        private const string LogFileName = "OpenScmClientAppilication";

        public void Start()
        {
            InitApplication();
            StartServices();
        }

        private void StartServices()
        {
            
        }

        private void InitApplication()
        {
            ILogService log = new LogService();
            log.LogOperation(LogFileName,"Begin Load Defalut Service");

            _container = new UServiceProvider();
            IUContainerService interContainer = (IUContainerService)_container;
            interContainer.AddInstanceService<ILogService>(log);

            interContainer.AddSingletonService<IProgramUpdateService, ProgramUpdateService>();

            interContainer.AddSingletonService<ITaskDispatchServiceContainer, TaskDispatchServiceContainer>();

        }

        /// <summary>
        /// 
        /// </summary>
        public void Pause()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void Continue()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            StopServices();
        }

        private void StopServices()
        {
            throw new NotImplementedException();
        }
    }
}
