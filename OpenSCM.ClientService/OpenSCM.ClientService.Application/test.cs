using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    public interface Itest
    {
        void test();
    }
    class test : Itest
    {
        void Itest.test()
        {
            Console.WriteLine("test ");
        }
    }
}
