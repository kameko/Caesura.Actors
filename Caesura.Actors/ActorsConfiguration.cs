
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorsConfiguration
    {
        public bool VerboseLogAllMessages { get; set; }
        public bool LogLostLetters { get; set; }
        
        public bool ManuallyStartScheduler { get; set; }
        public bool ParallelScheduler { get; set; }
        public int ParallelSchedulerMaxThreads { get; set; }
        public bool SpinDownFreeScheduler { get; set; }
        
        public IEnumerable<RemoteNoteConfiguration> RemoteNodes { get; set; }
        
        public ActorsConfiguration()
        {
            VerboseLogAllMessages       = false;
            LogLostLetters              = true;
            
            ManuallyStartScheduler      = false;
            ParallelScheduler           = false;
            ParallelSchedulerMaxThreads = -1;
            SpinDownFreeScheduler       = false;
            
            RemoteNodes                 = new List<RemoteNoteConfiguration>();
        }
        
        internal static ActorsConfiguration CreateDefault()
        {
            var config = new ActorsConfiguration
            {
                
            };
            return config;
        }
        
        public class RemoteNoteConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
            
            public RemoteNoteConfiguration(string ip, int port)
            {
                IPAddress = ip;
                Port      = port;
            }
        }
    }
}
