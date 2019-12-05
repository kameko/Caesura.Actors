
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class BaseHandler
    {
        internal virtual bool Handle(object raw_data)
        {
            return false;
        }
    }
    
    public class HandleAny : BaseHandler
    {
        private Actor Owner { get; set; }
        private Action<object>? HandlerCallback { get; set; }
        private Func<object, Task>? HandlerCallbackAsync { get; set; }
        
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
            if (!(handler.HandlerCallbackAsync is null))
            {
                throw new InvalidOperationException("Cannot set a handler if an async handler is already set.");
            }
            
            handler.HandlerCallback = handler_callback;
            return handler;
        }
        
        public static HandleAny operator + (HandleAny handler, Func<object, Task> handler_callback)
        {
            if (!(handler.HandlerCallback is null))
            {
                throw new InvalidOperationException("Cannot set an async handler if a handler is already set.");
            }
            
            handler.HandlerCallbackAsync = handler_callback;
            return handler;
        }
        
        internal override bool Handle(object raw_data)
        {
            try
            {
                HandlerCallback?.Invoke(raw_data);
                // TODO: handle async handler
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
    
    public class Handler<T> : BaseHandler
    {
        protected Actor Owner { get; set; }
        protected Predicate<T>? CanHandle { get; set; }
        protected Action<T>? HandlerCallback { get; set; }
        protected Func<T, Task>? HandlerCallbackAsync { get; set; }
        
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
            if (!(handler.HandlerCallbackAsync is null))
            {
                throw new InvalidOperationException("Cannot set a handler if an async handler is already set.");
            }
            
            handler.HandlerCallback = handler_callback;
            return handler;
        }
        
        public static Handler<T> operator + (Handler<T> handler, Func<T, Task> handler_callback)
        {
            if (!(handler.HandlerCallback is null))
            {
                throw new InvalidOperationException("Cannot set an async handler if a handler is already set.");
            }
            
            handler.HandlerCallbackAsync = handler_callback;
            return handler;
        }
        
        internal override bool Handle(object raw_data)
        {
            if (HandlerCallback is null && HandlerCallbackAsync is null)
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
                            Process(data);
                            return true;
                        }
                        catch (Exception e)
                        {
                            Owner.InternalLog.Error(e, $"Handler threw an exception");
                            // Errored or not, the message was handled,
                            // so we don't want to pass it on to any other
                            // callbacks.
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Owner.InternalLog.Error(e, $"Handle checker threw an exception");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        protected void Process(T data)
        {
            HandlerCallback?.Invoke(data);
            // TODO: handle async handler
        }
    }
    
    public class Handler : Handler<object>
    {
        internal Handler(Actor owner) : base(owner)
        {
            
        }
        
        public static new Handler Create(Actor owner)
        {
            return new Handler(owner);
        }
        
        public static Handler operator + (Handler handler, Predicate<object> can_handle)
        {
            handler.CanHandle = can_handle;
            return handler;
        }
        
        public static Handler operator + (Handler handler, Action<object> handler_callback)
        {
            if (!(handler.HandlerCallbackAsync is null))
            {
                throw new InvalidOperationException("Cannot set a handler if an async handler is already set.");
            }
            
            handler.HandlerCallback = handler_callback;
            return handler;
        }
        
        public static Handler operator + (Handler handler, Func<object, Task> handler_callback)
        {
            if (!(handler.HandlerCallback is null))
            {
                throw new InvalidOperationException("Cannot set an async handler if a handler is already set.");
            }
            
            handler.HandlerCallbackAsync = handler_callback;
            return handler;
        }
        
        internal override bool Handle(object raw_data)
        {
            if (HandlerCallback is null && HandlerCallbackAsync is null)
            {
                return false;
            }
            
            try
            {
                if (CanHandle is null || CanHandle.Invoke(raw_data))
                {
                    try
                    {
                        Process(raw_data);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Owner.InternalLog.Error(e, $"Handler threw an exception");
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Owner.InternalLog.Error(e, $"Handle checker threw an exception");
                return false;
            }
        }
    }
}
