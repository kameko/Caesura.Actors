
namespace Caesura.Actors.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    // TODO: make sure actors can transparently change what system they're
    // on from the view of the reference. The reference shouldn't change,
    // just the actor.
    
    public class RemoteActorReference : IActorReference
    {
        public ActorPath Path { get; private set; }
        
        public RemoteActorReference(ActorPath path)
        {
            Path = path;
        }
        
        public void Tell<T>(T data, IActorReference sender)
        {
            throw new NotImplementedException();
        }
        
        public void InformError(IActorReference sender, Exception e)
        {
            throw new NotImplementedException();
        }
        
        public void Destroy()
        {
            throw new NotImplementedException();
        }
    }
}
