
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class Fault
    {
        private ActorSystem System { get; set; }
        private ActorPath Receiver { get; set; }
        public IActorReference FaultedActor { get; private set; }
        public Exception Exception { get; private set; }
        
        public Fault(ActorSystem system, ActorPath receiver, IActorReference faulted_actor, Exception exception)
        {
            System       = system;
            Receiver     = receiver;
            FaultedActor = faulted_actor;
            Exception    = exception;
        }
        
        public void Restart()
        {
            // TODO: tell system to restart the actor
        }
        
        public void Destroy()
        {
            // TODO: do nothing really, just log it.
        }
        
        public void Escelate()
        {
            // TODO: send this to the parent
        }
    }
}
