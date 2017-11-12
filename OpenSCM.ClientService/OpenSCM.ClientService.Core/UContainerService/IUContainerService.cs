using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Core
{
    public interface IUContainerService
    {
        void AddService<TInterface, T>() where T : TInterface;

        void RemoveService<TInterface, T>() where T : TInterface;

        void AddSingletonService<TInterface, T>() where T : TInterface;
        //bool RemoveService(ResourceServiceKey key);

        //void AddSingletonInstanceService(Type t, object instance);

        void AddInstanceService<TInterface>(TInterface instance)

        void Dispose();
    }
}
