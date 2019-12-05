
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class LocalActorReference : IActorReference
    {
        private ActorSystem System;
        public ActorPath Path { get; private set; }
        
        public LocalActorReference(ActorSystem system, string path)
        {
            System = system;
            Path   = new ActorPath(path);
        }
        
        public LocalActorReference(ActorSystem system, ActorPath path)
        {
            System = system;
            Path   = path;
        }
        
        public void Tell<T>(T data, IActorReference sender)
        {
            System.EnqueueForMessageProcessing<T>(Path, data, sender);
        }
        
        public void Ask<T>(T data, IActorReference sender, Action<ActorContinuation, T> continue_with)
        {
            // TODO: have this send a special internal message type that will get sent back
            // to the actor that requested it and run the callback inside of the actor.
            
            throw new NotImplementedException();
        }
        
        public void Ask<T>(T data, IActorReference sender, Action<ActorContinuation, T> continue_with, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        
        public R Ask<T, R>(T data, IActorReference sender, Func<ActorContinuation, T, R> continue_with)
        {
            throw new NotImplementedException();
        }
        
        public R Ask<T, R>(T data, IActorReference sender, Func<ActorContinuation, T, R> continue_with, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        
        public void InformError(IActorReference sender, Exception e)
        {
            System.InformUnhandledError(Path, sender, e);
        }
        
        public void Destroy()
        {
            System.DestroyActor(this);
        }
    }
}
