
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class ActorCell
    {
        public Actor Owner { get; set; }
        public Type HandlerType { get; set; }
        
        public ActorCell(Actor owner, Type type)
        {
            Owner       = owner;
            HandlerType = type;
        }
    }
    
    internal class ActorCell<T> : ActorCell
    {
        private Predicate<T>? CanHandle { get; set; }
        private Action<T> Handler { get; set; }
        
        public ActorCell(Actor owner, Action<T> handler) : base(owner, typeof(T))
        {
            Handler = handler;
        }
        
        public ActorCell(Actor owner, Predicate<T> can_handle, Action<T> handler) : base(owner, typeof(T))
        {
            CanHandle = can_handle;
            Handler = handler;
        }
        
        public bool ShouldHandle(T message)
        {
            if (CanHandle is null)
            {
                return true;
            }
            return CanHandle.Invoke(message);
        }
        
        public void Handle(T message)
        {
            Handler.Invoke(message);
        }
    }
    
    internal class AsyncActorCell<T> : ActorCell
    {
        private Predicate<T>? CanHandle { get; set; }
        private Func<T, Task> Handler { get; set; }
        
        public AsyncActorCell(Actor owner, Func<T, Task> handler) : base(owner, typeof(T))
        {
            Handler = handler;
        }
        
        public AsyncActorCell(Actor owner, Predicate<T> can_handle, Func<T, Task> handler) : base(owner, typeof(T))
        {
            CanHandle = can_handle;
            Handler = handler;
        }
        
        public bool ShouldHandle(T message)
        {
            if (CanHandle is null)
            {
                return true;
            }
            return CanHandle.Invoke(message);
        }
        
        public Task Handle(T message)
        {
            return Handler.Invoke(message);
        }
    }
}
