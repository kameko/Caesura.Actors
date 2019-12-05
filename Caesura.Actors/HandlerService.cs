
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class HandlerService
    {
        private Actor Owner { get; set; }
        private List<BaseHandler> Handlers { get; set; }
        
        internal HandlerService(Actor owner)
        {
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
                return ActorProcessingResult.Unhandled;
            }
        }
        
        internal void Clear()
        {
            Handlers.Clear();
        }
    }
}
