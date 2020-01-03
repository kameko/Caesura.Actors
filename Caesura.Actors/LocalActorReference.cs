
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
        
        public Task<Context<T>> Ask<T>(T data, IActorReference sender)
        {
            return Ask<T, T>(data, sender, TimeSpan.FromSeconds(5));
        }
        
        // NOTICE: we just aren't doing an Ask method
        public Task<Context<R>> Ask<T, R>(T data, IActorReference sender, TimeSpan timeout)
        {
            var token = new CancellationTokenSource();
            
            // Tell<T>(data, sender);
            
            // TODO: we need to put the scheduler's container for the sender
            // in some sort of state where it will only accept a message of type
            // R from this actor.
            // No, actually just return R. The problem is how to get it out of this actor reference.
            
            // TODO: we'll have tell the scheduler this is an Ask message and intercept it when it runs,
            // then instead of Telling the sender normally, the scheduler will just return the message
            // from here as a Task<R>
            
            // TODO: maybe instead, we'll put the state machine for Ask'ing inside the sender actor itself
            
            var task = Task<R>.Run<R>(() =>
            {
                // TODO: tell the sender?
                // we might need to run some of the scheduler manually in here
                return null!;
            }
            , token.Token)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // TODO: escelate errors
                }
                
            });
            
            token.CancelAfter(timeout);
            // return task;
            
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
        
        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
