using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Resolution;

namespace OpenSCM.ClientService.Core
{
    public interface IUServiceProvider
    {
        T GetService<T>();

        T GetService<T>(string key);

        T GetService<T>(params ParameterOverride[] obj);

        T GetService<T>(string key,params ParameterOverride[] obj);
    }
}
