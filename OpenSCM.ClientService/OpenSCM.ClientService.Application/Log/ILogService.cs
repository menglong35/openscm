using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    public interface ILogService
    {
        void LogException(string category, string content, Exception ex);
        void LogException(string content, Exception ex);

        void LogOperation(string category, string content);
        void LogOperation(string content);
    }
}
