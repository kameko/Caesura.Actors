
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class HandlerService
    {
        private Actor Owner { get; set; }
        private List<Handler> Handlers { get; set; }
        
        internal HandlerService(Actor owner)
        {
            Owner    = owner;
            Handlers = new List<Handler>();
        }
        
        internal void Add(Handler handler)
        {
            // TODO: throw on more than one HandleAny
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
                    if (any_handler.Handle(data))
                    {
                        handled = true;
                    }
                }
            }
            
            // TODO: handle unprocessed messages
            return ActorProcessingResult.Success;
        }
        
        internal void Clear()
        {
            Handlers.Clear();
        }
    }
}
