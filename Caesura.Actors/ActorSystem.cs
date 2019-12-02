
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorSystem
    {
        public string Name { get; private set; }
        private Dictionary<ActorPath, Actor> Actors { get; set; }
        
        public ActorSystem(string name)
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
            throw new NotImplementedException();
        }
        
        public void DestroyActor(ActorPath path)
        {
            throw new NotImplementedException();
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
        
        internal void CreateChildActor(Actor actor)
        {
            Actors.Add(actor.Path, actor);
        }
    }
}
