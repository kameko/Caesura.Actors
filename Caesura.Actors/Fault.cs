
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class Fault
    {
        public IActorReference FaultedActor { get; private set; }
        public Exception Exception { get; private set; }
        
        public Fault(IActorReference actor, Exception exception)
        {
            FaultedActor = actor;
            Exception    = exception;
        }
    }
}
