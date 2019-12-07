
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
        
        public ActorsConfiguration()
        {
            VerboseLogAllMessages  = false;
            ManuallyStartScheduler = false;
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
