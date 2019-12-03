
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
        public ActorPath Location { get; private set; }
        private Dictionary<ActorPath, Actor> Actors { get; set; }
        
        internal ActorSystem(string name)
        {
            Name     = ActorPath.Sanitize(name);
            Location = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors   = new Dictionary<ActorPath, Actor>();
            
            var root = new RootSupervisor(this);
            root.Populate(this, ActorReferences.Nobody, Location);
            Actors.Add(Location, root);
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
                
                var actor = Actors[path];
                
                if (Actors.ContainsKey(actor.InternalParent.Path))
                {
                    // remove self from parent
                    var parent = Actors[actor.InternalParent.Path];
                    var child_to_remove = parent.InternalChildren.Find(x => x.Path == path);
                    if (child_to_remove is { })
                    {
                        parent.InternalChildren.Remove(child_to_remove);
                    }
                }
                
                // recursively destroy children
                foreach (var child in actor.InternalChildren)
                {
                    var cpath = child.Path;
                    DestroyActor(path);
                }
                
                Actors.Remove(path);
            }
        }
        
        private void OnDestroyActor(ActorPath path)
        {
            if (Actors.ContainsKey(path))
            {
                var actor = Actors[path];
                actor.CallOnDestruction();
            }
        }
        
        public void DestroyActor(string path) => DestroyActor(new ActorPath(path));
        
        public void Shutdown()
        {
            throw new NotImplementedException();
        }
        
        internal void EnqueueWait(Actor actor, TimeSpan time, Action continue_with)
        {
            throw new NotImplementedException();
        }
        
        internal void EnqueueForMessageProcessing<T>(ActorPath path, T data, IActorReference sender)
        {
            if (!Actors.ContainsKey(path))
            {
                // TODO: ask the network if they have this actor, otherwise...
                Unhandled(sender, (object)data!);
                return;
            }
            
            throw new NotImplementedException();
        }
        
        internal void Unhandled(IActorReference sender, object message)
        {
            throw new NotImplementedException();
        }
        
        internal void InformUnhandledError(ActorPath receiver, IActorReference faulted_actor, Exception e)
        {
            if (!Actors.ContainsKey(receiver))
            {
                return;
            }
            
            var rec = Actors[receiver];
            var msg = new Fault(faulted_actor, e);
            EnqueueForMessageProcessing(receiver, msg, faulted_actor);
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
