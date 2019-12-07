
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorsConfiguration
    {
        public bool VerboseLogAllMessages { get; set; }
        public bool ManuallyStartScheduler { get; set; }
        public bool LogLostLetters { get; set; }
        public bool ParallelScheduler { get; set; }
        
        public ActorsConfiguration()
        {
            VerboseLogAllMessages  = false;
            ManuallyStartScheduler = false;
            LogLostLetters         = true;
            ParallelScheduler      = false;
        }
        
        internal static ActorsConfiguration CreateDefault()
        {
            var config = new ActorsConfiguration
            {
                
            };
            return config;
        }
    }
}
