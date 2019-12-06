
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
        
        internal Fault(ActorSystem system, ActorPath receiver, IActorReference faulted_actor, Exception exception)
        {
            System       = system;
            Receiver     = receiver;
            FaultedActor = faulted_actor;
            Exception    = exception;
        }
        
        public void Restart()
        {
            System.RestartFaultedActor(Receiver, FaultedActor);
        }
        
        public void Destroy()
        {
            System.DestroyFaultedActor(Receiver, FaultedActor);
        }
        
        public void DestroyAllChildren()
        {
            var receiver = System.GetActor(Receiver);
            if (!(receiver is null))
            {
                foreach (var child in receiver.Actor.InternalChildren)
                {
                    System.DestroyActor(child);
                }
            }
        }
        
        public void Escelate()
        {
            var receiver = System.GetActor(Receiver);
            if (!(receiver is null))
            {
                receiver.Actor.InternalParent.InformError(FaultedActor, Exception);
            }
        }
    }
}
