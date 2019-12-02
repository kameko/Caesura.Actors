
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
        
        public void Tell<T>(T data)
        {
            System.Tell<T>(Path, data);
        }
        
        public void Tell<T>(T data, IActorReference sender)
        {
            System.Tell<T>(Path, data, sender);
        }
        
        public Task Ask<T>(T data)
        {
            throw new NotImplementedException();
        }
        
        public Task Ask<T>(T data, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        
        public Task<R> Ask<T, R>(T data)
        {
            throw new NotImplementedException();
        }
        
        public Task<R> Ask<T, R>(T data, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        
        public void InformUnhandledError(Exception e)
        {
            throw new NotImplementedException();
        }
    }
}
