
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class HandlerService
    {
        private ActorSystem System { get; set; }
        private Actor Owner { get; set; }
        private List<BaseHandler> Handlers { get; set; }
        internal object? CurrentMessage { get; set; }
        
        internal HandlerService(ActorSystem system, Actor owner)
        {
            System   = system;
            Owner    = owner;
            Handlers = new List<BaseHandler>();
        }
        
        internal void Add(BaseHandler handler)
        {
            if (Handlers.Exists(x => x is HandleAny))
            {
                throw new ArgumentException($"Handlers already contains a {nameof(HandleAny)}");
            }
            
            Handlers.Add(handler);
        }
        
        internal ActorProcessingResult Handle(object data)
        {
            CurrentMessage = data;
            var handled = false;
            
            foreach (var handler in Handlers)
            {
                if (handler.Handle(data))
                {
                    handled = true;
                    break;
                }
            }
            
            if (!handled)
            {
                var any_handler = Handlers.Find(x => x is HandleAny);
                if (any_handler is HandleAny)
                {
                    handled = true;
                    any_handler.Handle(data);
                }
            }
            
            if (handled)
            {
                return ActorProcessingResult.Success;
            }
            else
            {
                if (data is Fault fault)
                {
                    // the missed message was a Fault, which means
                    // the child actor threw an exception and the
                    // parent didn't handle it, so now it's the
                    // parent's exception.
                    
                    Owner.InformParentOfError(fault.Exception);
                    System.FaultedActor(Owner, fault.Exception);
                }
                
                return ActorProcessingResult.Unhandled;
            }
        }
        
        internal void Clear()
        {
            Handlers.Clear();
        }
    }
}
