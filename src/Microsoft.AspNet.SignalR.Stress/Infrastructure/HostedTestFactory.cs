using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
{
    public class HostedTestFactory
    {
        public static ITestHost CreateHost(string Host, string Transport, string ScenarioName, string Url)
        {
            return new MemoryHost(Transport);
        }
    }
}
