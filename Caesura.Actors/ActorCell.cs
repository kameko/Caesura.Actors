
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorCell
    {
        public Actor Owner { get; set; }
        
        public ActorCell(Actor owner)
        {
            Owner = owner;
        }
    }
    
    public class ActorCell<T> : ActorCell
    {
        private Predicate<T> CanHandle { get; set; }
        private Action<T> Handler { get; set; }
        
        public ActorCell(Actor owner, Predicate<T> canHandle, Action<T> handler) : base(owner)
        {
            CanHandle = canHandle;
            Handler = handler;
        }
        
        public bool ShouldHandle(T message)
        {
            return CanHandle.Invoke(message);
        }
        
        public void Handle(T message)
        {
            Handler.Invoke(message);
        }
    }
}
