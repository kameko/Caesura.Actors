
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
        
        internal void BeginSessionPersistence(Actor actor)
        {
            // TODO: tell the scheduler we're not done processing.
            throw new NotImplementedException();
        }
        
        internal void EndSessionPersistence(Actor actor)
        {
            // TODO: tell the scheduler we're ready for a new message.
            throw new NotImplementedException();
        }
    }
}
