using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    class ClientConfigService : IClientConfigService
    {
        private ClientConfig _clientConfig;
        public ClientConfig Config {
            get
            {
                if (_clientConfig == null)
                {
                    _clientConfig = ReadConfig<ClientConfig>(PathUtils.ClientConfigPath);
                }
                return _clientConfig;
            }
        }

        private T ReadConfig<T>(string path)
        {
            try
            {
                return SerializationUtils.XmlDeserializeFromFile<T>(path);
            }
            catch (Exception)
            {
                T newCreate  = default(T);
                WriteConfig(PathUtils.ClientConfigPath,newCreate);
                return newCreate;
            }
        }

        public bool WriteConfig(string path,object config)
        {
            try
            {
                File.WriteAllText(path, SerializationUtils.XmlSerialize(config));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
