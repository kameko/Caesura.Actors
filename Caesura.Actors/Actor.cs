
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    
    // TODO: stall/wait method that doesn't use async but still
    // allows the system to process other messages while it keeps
    // the actor in a non-blocking stasis.
    
    public abstract class Actor : ITellable
    {
        public ActorPath Path { get; private set; }
        private ActorSystem System { get; set; }
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
            System           = system;
            Self             = new LocalActorReference(system, path);
            InternalParent   = parent;
            InternalChildren = new List<IActorReference>();
            Stash            = new MessageStash(this);
            Cells            = new List<ActorCell>();
        }
        
        public virtual void PreReload()
        {
            
        }
        
        public virtual void PostReload()
        {
            
        }
        
        public virtual void OnDestruction()
        {
            
        }
        
        protected void Tell<T>(IActorReference actor, T data)
        {
            actor.Tell(data, Self);
        }
        
        protected void Ask<T>(IActorReference actor, T data, Action<T> continueWith)
        {
            actor.Ask(data, Self, continueWith);
        }
        
        protected void Ask<T>(IActorReference actor, T data, Action<T> continueWith, TimeSpan timeout)
        {
            actor.Ask(data, Self, continueWith, timeout);
        }
        
        protected R Ask<T, R>(IActorReference actor, T data, Func<T, R> continueWith)
        {
            return actor.Ask(data, Self, continueWith);
        }
        
        protected R Ask<T, R>(IActorReference actor, T data, Func<T, R> continueWith, TimeSpan timeout)
        {
            return actor.Ask(data, Self, continueWith, timeout);
        }
        
        protected void Forward<T>(IActorReference actor, T data)
        {
            actor.Tell(data, Sender);
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
        
        protected void Receive<T>(Predicate<T> canHandle, Action<T> handler)
        {
            var cell = new ActorCell<T>(this, canHandle, handler);
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
        
        internal void ProcessMessage<T>(IActorReference sender, T message)
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
            
            if (!handled)
            {
                Unhandled();
            }
        }
        
        protected void Unhandled()
        {
            if (CurrentMessage is { })
            {
                System.Unhandled(CurrentMessage);
            }
        }
        
        protected void DestroySelf()
        {
            System.DestroyActor(Self);
        }
        
        internal void InformParentOfUnhandledError(Exception e)
        {
            InternalParent.InformUnhandledError(e);
        }
    }
}
