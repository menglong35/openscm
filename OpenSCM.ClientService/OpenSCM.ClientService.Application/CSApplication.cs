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

        public void Start()
        {
            InitApplication();
            StartServices();
        }

        private void StartServices()
        {
            _container.GetService<ITest2>().SayHello();

        }

        private void InitApplication()
        {
            _container = new UServiceProvider();
            IUContainerService interContainer = (IUContainerService)_container;


            interContainer.AddService<Itest, test>();
            interContainer.AddService<ITest2, Test2>();
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
