
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    // NOTICE: not needed, was for the Ask method which is not being used.
    public class Context
    {
        public IActorReference Sender { get; internal set; }
        
        internal Context()
        {
            Sender = ActorReferences.NoSender;
        }
        
        internal Context(IActorReference sender)
        {
            Sender = sender;
        }
    }
    
    public class Context<T> : Context
    {
        public T Message { get; internal set; }
        
        internal Context()
        {
            Message = default!;
        }
        
        
    }
}
