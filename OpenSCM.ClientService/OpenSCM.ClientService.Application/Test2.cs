using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Attributes;

namespace OpenSCM.ClientService.Application
{
    public interface ITest2
    {
        void SayHello();
    }
    class Test2 : ITest2
    {
        [Dependency]
        public Itest Te { get; set; }

        public void SayHello()
        {
            Console.WriteLine("say hello from test2"+";");
            Console.WriteLine("test say what:");
            Te.test();
        }
    }
}
