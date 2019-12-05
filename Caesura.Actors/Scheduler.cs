
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    
    
    
    internal class Scheduler
    {
        internal ActorSystem System { get; set; }
        private List<ActorQueueToken> Queue { get; set; }
        
        public Scheduler(ActorSystem system)
        {
            System = system;
            Queue  = new List<ActorQueueToken>();
        }
        
        // TODO: use Task.Run
    }
}
