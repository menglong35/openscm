using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    public interface IClientConfigService
    {
        ClientConfig Config { get; }

        bool WriteConfig(string path, object config);
    }
}
