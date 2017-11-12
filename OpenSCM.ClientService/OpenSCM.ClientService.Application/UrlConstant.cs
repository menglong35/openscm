using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{

    public static class UrlConstant
    {

        private static readonly string _openSCMUrl = "https://www.openscm.net";

        private static readonly string _openSCMApiUrl = "{0}/api";

        private static readonly string _updateApiUrl = "{0}/urlforupdate?ticket={1}";

        private static readonly string _openSCMDefaultUpdateUrl = "https://update.openscm.net/";

        public static string OpenSCMUrl
        {
            get { return _openSCMUrl; }
        }
        //https://www.openscm.net/api
        public static string OpenSCMApiUrl
        {
            get { return string.Format(_openSCMApiUrl,_openSCMUrl); }
        }

        //https://www.openscm.net/api/urlforupdate?ticket={1}
        public static string OpenSCMUpdateApiUrl => string.Format(_updateApiUrl, OpenSCMApiUrl);

        public static string OpenSCMDefaultUpdateUrl
        {
            get { return _openSCMDefaultUpdateUrl; }
        }
    }
}
