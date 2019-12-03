
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
        protected IActorReference Parent { get; private set; }
        protected IReadOnlyList<IActorReference> Children { get; private set; }
        protected Stash Stash { get; private set; }
        private List<ActorCell> Cells { get; set; }
        private Action<object>? OnAny { get; set; }
        private object? CurrentMessage { get; set; }
        
        internal Actor()
        {
            Path     = null!;
            System   = null!;
            Self     = null!;
            Parent   = null!;
            Children = null!;
            Stash    = null!;
            Cells    = null!;
        }
        
        internal void Populate(ActorSystem system, IActorReference parent, ActorPath path)
        {
            Path     = path;
            Self     = new LocalActorReference(system, path);
            System   = system;
            Parent   = parent;
            Children = new List<IActorReference>();
            Stash    = new Stash(this);
            Cells    = new List<ActorCell>();
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
            return System.CreateChildActor(this, schematic, name);
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
                    InformParentOfUnhandledError(e);
                    break;
                }
            }
            
            if (!handled)
            {
                if (!(OnAny is null))
                {
                    handled = true;
                    try
                    {
                        OnAny.Invoke((object)message!);
                    }
                    catch (Exception e)
                    {
                        InformParentOfUnhandledError(e);
                    }
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
            Parent.InformUnhandledError(e);
        }
    }
}
