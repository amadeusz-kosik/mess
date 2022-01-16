using System.ServiceModel;
using System.Threading;

using System;

namespace Supervisor.Server
{
    class Program
    {

        static void Main(string[] args)
        {
            using (ServiceHost serviceHost = new ServiceHost(typeof(HyperVService)))
            {                
                serviceHost.Open();
                Console.Out.WriteLine("Starting REST service.");

                EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
                eventWaitHandle.WaitOne();
            }
            
        }
    }
}