
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class Handler
    {
        // TODO: replicate Handle<T> but with objects
        
        internal virtual bool Handle(object raw_data)
        {
            return false;
        }
    }
    
    public class HandleAny : Handler
    {
        private Actor Owner { get; set; }
        private Action<object>? HandlerCallback { get; set; }
        
        internal HandleAny(Actor owner)
        {
            Owner = owner;
        }
        
        public static HandleAny Create(Actor owner)
        {
            return new HandleAny(owner);
        }
        
        public static HandleAny operator + (HandleAny handler, Action<object> handler_callback)
        {
            handler.HandlerCallback = handler_callback;
            return handler;
        }
        
        internal override bool Handle(object raw_data)
        {
            try
            {
                HandlerCallback?.Invoke(raw_data);
            }
            catch
            {
                // TODO: log error
            }
            
            // Always return true. If a HandleAny simply exists
            // at all then the message was handled.
            return true;
        }
    }
    
    public class Handler<T> : Handler
    {
        private Actor Owner { get; set; }
        private Predicate<T>? CanHandle { get; set; }
        private Action<T>? HandlerCallback { get; set; }
        
        internal Handler(Actor owner)
        {
            Owner = owner;
            Owner.Handlers.Add(this);
        }
        
        public static Handler<T> Create(Actor owner)
        {
            return new Handler<T>(owner);
        }
        
        public static Handler<T> operator + (Handler<T> handler, Predicate<T> can_handle)
        {
            handler.CanHandle = can_handle;
            return handler;
        }
        
        public static Handler<T> operator + (Handler<T> handler, Action<T> handler_callback)
        {
            handler.HandlerCallback = handler_callback;
            return handler;
        }
        
        public static Handler<T> operator + (Handler<T> handler, Func<T, Task> handler_callback)
        {
            throw new NotImplementedException();
        }
        
        internal override bool Handle(object raw_data)
        {
            if (HandlerCallback is null)
            {
                return false;
            }
            
            if (raw_data is T data)
            {
                try
                {
                    if (CanHandle is null || CanHandle.Invoke(data))
                    {
                        try
                        {
                            HandlerCallback.Invoke(data);
                        }
                        catch
                        {
                            // TODO: log error
                            
                            // Errored or not, the message was handled,
                            // so we don't want to pass it on to any other
                            // callbacks.
                            return true;
                        }
                    }
                }
                catch
                {
                    // TODO: log error
                }
            }
            
            return false;
        }
    }
}
