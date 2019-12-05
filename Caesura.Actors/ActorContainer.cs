
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal enum ActorStatus
    {
        Ready,
        Enqueued,
        Destroying,
        Destroyed,
    }
    
    internal class ActorContainer
    {
        public Actor Actor { get; set; }
        public ActorStatus Status { get; set; }
        
        public ActorContainer(Actor actor)
        {
            Status = ActorStatus.Ready;
            Actor = actor;
        }
    }
}
