
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    // TODO: multithread
    
    public class ActorSystem
    {
        public string Name { get; private set; }
        private Dictionary<ActorPath, Actor> Actors { get; set; }
        
        internal ActorSystem(string name)
        {
            Name = name;
            Actors = new Dictionary<ActorPath, Actor>();
        }
        
        public static ActorSystem Create(string name)
        {
            return new ActorSystem(name);
        }
        
        public IActorReference NewActor(ActorSchematic schematic, string name)
        {
            throw new NotImplementedException();
        }
        
        public void DestroyActor(IActorReference reference)
        {
            var paths = Actors.Where(x => x.Key == reference.Path);
            if (paths.Count() > 0)
            {
                var path = paths.First().Key;
                DestroyActor(path);
            }
        }
        
        public void DestroyActor(ActorPath path)
        {
            if (Actors.ContainsKey(path))
            {
                OnDestroyActor(path);
                Actors.Remove(path);
            }
        }
        
        private void OnDestroyActor(ActorPath path)
        {
            try
            {
                var actor = Actors[path];
                actor.OnDestruction();
            }
            catch
            {
                // TODO: log exception.
            }
        }
        
        public void DestroyActor(string path) => DestroyActor(new ActorPath(path));
        
        public void Tell<T>(ActorPath path, T data, IActorReference sender)
        {
            throw new NotImplementedException();
        }
        
        public void Tell<T>(ActorPath path, T data)
        {
            throw new NotImplementedException();
        }
        
        public void Tell<T>(string path, T data) => Tell<T>(new ActorPath(path), data);
        
        internal void Unhandled(object message)
        {
            throw new NotImplementedException();
        }
        
        internal IActorReference CreateChildActor(Actor parent, ActorSchematic child, string child_name)
        {
            var path = new ActorPath(parent.Path.Path, child_name);
            var self = new LocalActorReference(this, parent.Path);
            var actor = child.Create();
            actor.Populate(this, self, path);
            Actors.Add(actor.Path, actor);
            return new LocalActorReference(this, actor.Path);
        }
    }
}
