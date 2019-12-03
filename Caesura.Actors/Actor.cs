
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    
    public abstract class Actor : ITellable
    {
        public ActorPath Path { get; private set; }
        private ActorSystem System { get; set; }
        protected ActorLogger ActorLog { get; private set; }
        protected IActorReference Sender { get; private set; }
        protected IActorReference Self { get; private set; }
        protected IActorReference Parent => InternalParent;
        protected IReadOnlyList<IActorReference> Children => InternalChildren;
        protected MessageStash Stash { get; private set; }
        private List<ActorCell> Cells { get; set; }
        private Action<object>? OnAny { get; set; }
        private object? CurrentMessage { get; set; }
        internal IActorReference InternalParent { get; set; }
        internal List<IActorReference> InternalChildren { get; set; }
        
        public Actor()
        {
            Path             = null!;
            ActorLog         = null!;
            System           = null!;
            Sender           = null!;
            Self             = null!;
            InternalParent   = null!;
            InternalChildren = null!;
            Stash            = null!;
            Cells            = null!;
        }
        
        internal void Populate(ActorSystem system, IActorReference parent, ActorPath path)
        {
            Path             = path;
            ActorLog         = new ActorLogger(this);
            System           = system;
            Self             = new LocalActorReference(system, path);
            InternalParent   = parent;
            InternalChildren = new List<IActorReference>();
            Stash            = new MessageStash(this);
            Cells            = new List<ActorCell>();
        }
        
        protected virtual void PreReload()
        {
            
        }
        
        protected virtual void PostReload()
        {
            
        }
        
        protected virtual void OnDestruction()
        {
            
        }
        
        // return Success if it didn't error so we can call PostReload,
        // otherwise kill the actor.
        internal ActorProcessingResult CallPreReload()
        {
            try
            {
                PreReload();
                return ActorProcessingResult.Success;
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(PreReload)}");
                return ActorProcessingResult.Errored;
            }
        }
        
        // If this returns Errored, we destroy the actor. Otherwise
        // it would be an endless loop of erroring.
        internal ActorProcessingResult CallPostReload()
        {
            try
            {
                PostReload();
                return ActorProcessingResult.Success;
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(PostReload)}");
                return ActorProcessingResult.Errored;
            }
        }
        
        // We don't care if this works or not because the actor is
        // getting destroyed anyway.
        internal void CallOnDestruction()
        {
            try
            {
                OnDestruction();
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(OnDestruction)}");
            }
        }
        
        protected void Respond<T>(T data)
        {
            Sender.Tell(data, Self);
        }
        
        protected void Tell<T>(IActorReference actor, T data)
        {
            actor.Tell(data, Self);
        }
        
        protected void Ask<T>(IActorReference actor, T data, Action<T> continue_with)
        {
            actor.Ask(data, Self, continue_with);
        }
        
        protected void Ask<T>(IActorReference actor, T data, Action<T> continue_with, TimeSpan timeout)
        {
            actor.Ask(data, Self, continue_with, timeout);
        }
        
        protected R Ask<T, R>(IActorReference actor, T data, Func<T, R> continue_with)
        {
            return actor.Ask(data, Self, continue_with);
        }
        
        protected R Ask<T, R>(IActorReference actor, T data, Func<T, R> continue_with, TimeSpan timeout)
        {
            return actor.Ask(data, Self, continue_with, timeout);
        }
        
        protected void Forward<T>(IActorReference actor, T data)
        {
            actor.Tell(data, Sender);
        }
        
        protected void Wait(TimeSpan time, Action continue_with)
        {
            System.EnqueueWait(this, time, continue_with);
        }
        
        protected IActorReference NewChild(ActorSchematic schematic, string name)
        {
            var child = System.CreateChildActor(this, schematic, name);
            return child;
        }
        
        protected void Become(Action method)
        {
            Cells.Clear();
            OnAny = null;
            method.Invoke();
        }
        
        protected void Receive<T>(Predicate<T> can_handle, Action<T> handler)
        {
            var cell = new ActorCell<T>(this, can_handle, handler);
            Cells.Add(cell);
        }
        
        protected void Receive<T>(Action<T> handler)
        {
            var cell = new ActorCell<T>(this, handler);
            Cells.Add(cell);
        }
        
        protected void ReceiveAny(Action<object> handler)
        {
            OnAny = handler;
        }
        
        internal ActorProcessingResult ProcessMessage<T>(IActorReference sender, T message)
        {
            Sender = sender;
            CurrentMessage = (object)message!;
            
            var handled = false;
            var errored = false;
            var cells = Cells.Where(x => x is ActorCell<T>) as IEnumerable<ActorCell<T>>;
            foreach (var cell in cells!)
            {
                try
                {
                    if (cell.ShouldHandle(message))
                    {
                        handled = true;
                        cell.Handle(message);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    errored = true;
                    InformParentOfUnhandledError(e);
                    break;
                }
            }
            
            // If no Cell handled the message without erroring, try handling it here.
            if ((!handled) && (!errored) && !(OnAny is null))
            {
                handled = true;
                try
                {
                    OnAny.Invoke((object)message!);
                }
                catch (Exception e)
                {
                    errored = true;
                    InformParentOfUnhandledError(e);
                }
            }
            
            if (errored)
            {
                return ActorProcessingResult.Errored;
            }
            else if (!handled)
            {
                Unhandled();
                return ActorProcessingResult.Unhandled;
            }
            else
            {
                return ActorProcessingResult.Success;
            }
        }
        
        protected void Unhandled()
        {
            if (CurrentMessage is { })
            {
                System.Unhandled(Sender, Self, CurrentMessage);
            }
        }
        
        protected void DestroySelf()
        {
            System.DestroyActor(Self);
        }
        
        internal void InformParentOfUnhandledError(Exception e)
        {
            InternalParent.InformUnhandledError(Self, e);
        }
    }
}
