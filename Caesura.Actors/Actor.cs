
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
        protected ActorSystem System { get; private set; }
        protected IActorReference Parent { get; private set; }
        protected IReadOnlyList<IActorReference> Children { get; private set; }
        protected Stash Stash { get; private set; }
        private List<ActorCell> Cells { get; set; }
        
        internal Actor()
        {
            Path     = null!;
            System   = null!;
            Parent   = null!;
            Children = null!;
            Stash    = null!;
            Cells    = null!;
        }
        
        internal void Populate(ActorSystem system, IActorReference parent, ActorPath path)
        {
            Path     = path;
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
        
        protected IActorReference NewChild(ActorSchematic schematic, string name)
        {
            var path = new ActorPath(Path.Path, name);
            var self = new LocalActorReference(System, Path);
            var actor = schematic.Create();
            actor.Populate(System, self, path);
            System.CreateChildActor(actor);
            return new LocalActorReference(System, actor.Path);
        }
        
        protected void Become(Action method)
        {
            Cells.Clear();
            method.Invoke();
        }
        
        protected void Receive<T>(Predicate<T> canHandle, Action<T> handler)
        {
            var cell = new ActorCell<T>(this, canHandle, handler);
            Cells.Add(cell);
        }
        
        internal void ProcessMessage<T>(T message)
        {
            var cells = Cells.Where(x => x is ActorCell<T>) as IEnumerable<ActorCell<T>>;
            foreach (var cell in cells!)
            {
                try
                {
                    if (cell.ShouldHandle(message))
                    {
                        cell.Handle(message);
                        break;
                    }
                }
                catch (Exception e)
                {
                    InformParentOfUnhandledError(e);
                }
            }
        }
        
        internal void InformParentOfUnhandledError(Exception e)
        {
            Parent.InformUnhandledError(e);
        }
    }
}
