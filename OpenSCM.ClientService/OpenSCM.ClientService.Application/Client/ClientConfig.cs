using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace OpenSCM.ClientService.Application
{

    /// <summary>
    /// 客户端配置
    /// </summary>
    [XmlType("ClientConfig")]
    public class ClientConfig
    {

        /// <summary>
        /// 
        /// </summary>
        public ClientConfig()
        {
            ProxyAddress = "";
            LicenseOperatorServiceAddress = "http://127.0.0.1";
            Databases = new Collection<string>();
        }

        /// <summary>
        /// 代理服务器地址
        /// </summary>
        [XmlElement("proxyAddress")]
        public string ProxyAddress
        {
            get;
            set;
        }

        /// <summary>
        /// 授权中心WebService地址
        /// </summary>
        [XmlElement("licenseOperatorServiceAddress")]
        public string LicenseOperatorServiceAddress
        {
            get;
            set;
        }

        /// <summary>
        /// 用户协议同意状态
        /// </summary>
        [XmlElement("AgreementState")]
        public bool AgreementState
        {
            get;
            set;
        }

        /// <summary>
        /// 数据库
        /// </summary>
        [XmlArray("Databases"), XmlArrayItem("Database", typeof(string))]
        public Collection<string> Databases
        {
            get;
            set;
        }

    }
}
