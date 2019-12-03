
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
        protected IActorReference Self { get; private set; }
        protected IActorReference Parent => InternalParent;
        protected IReadOnlyList<IActorReference> Children => InternalChildren;
        protected Stash Stash { get; private set; }
        private List<ActorCell> Cells { get; set; }
        private Action<object>? OnAny { get; set; }
        private object? CurrentMessage { get; set; }
        internal IActorReference InternalParent { get; set; }
        internal List<IActorReference> InternalChildren { get; set; }
        
        internal Actor()
        {
            Path             = null!;
            System           = null!;
            Self             = null!;
            InternalParent   = null!;
            InternalChildren = null!;
            Stash            = null!;
            Cells            = null!;
        }
        
        internal void Populate(ActorSystem system, IActorReference parent, ActorPath path)
        {
            Path             = path;
            Self             = new LocalActorReference(system, path);
            System           = system;
            InternalParent   = parent;
            InternalChildren = new List<IActorReference>();
            Stash            = new Stash(this);
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
        
        internal void ProcessMessage<T>(T message)
        {
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
