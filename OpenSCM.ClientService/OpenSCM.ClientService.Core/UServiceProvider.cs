using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity;
using Unity.Resolution;

namespace OpenSCM.ClientService.Core
{
    public class UServiceProvider : IUServiceProvider, IUContainerService, IDisposable
    {
        private readonly IUnityContainer _container = new UnityContainer();

        private object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType);
        }

        #region IUServiceProvider实现
        public T GetService<T>()
        {
            return _container.Resolve<T>();
        }

        public T GetService<T>(string key)
        {
            return _container.Resolve<T>(key);
        }

        public T GetService<T>(params ParameterOverride[] obj)
        {
            return _container.Resolve<T>(obj);
        }

        public T GetService<T>(string key, params ParameterOverride[] obj)
        {
            return _container.Resolve<T>(key, obj);
        }
        #endregion

        #region IUContainerService
        public void AddService<TInterface, T>() where T : TInterface
        {
            _container.RegisterType<TInterface, T>();
        }

        public void RemoveService<TInterface, T>() where T : TInterface
        {
            //_container.RegisterType<TInterface, T>();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
        #endregion

    }
}
