
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
        private List<ActorCell> Cells { get; set; }
        
        public Actor(ActorSystem system, IActorReference parent, string path)
        {
            Path = new ActorPath(path);
            System = system;
            Parent = parent;
            Children = new List<IActorReference>();
            Cells = new List<ActorCell>();
        }
        
        public virtual void PreReload()
        {
            
        }
        
        public virtual void PostReload()
        {
            
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
        
        public void ProcessMessage<T>(T message)
        {
            var cells = Cells.Where(x => x is ActorCell<T>) as IEnumerable<ActorCell<T>>;
            foreach (var cell in cells!)
            {
                if (cell.ShouldHandle(message))
                {
                    cell.Handle(message);
                    break;
                }
            }
        }
    }
}
