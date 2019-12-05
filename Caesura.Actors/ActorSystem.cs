
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    
    // TODO: scheduler
    // TODO: persistance handling, by saving the user's
    // IStateSerializeHandler/IStateDeserializeHandler
    // objects
    // TODO: config, including allowing a custom scheduler.
    
    public class ActorSystem : IDisposable
    {
        internal static int SystemCount { get; set; }
        
        public string Name { get; private set; }
        public ActorPath Location { get; private set; }
        private Dictionary<ActorPath, ActorContainer> Actors { get; set; }
        private Scheduler Scheduler { get; set; }
        internal ActorLogger Log { get; set; }
        
        internal RootSupervisor Root { get; set; }
        internal LostLetters Lost { get; set; }
        
        static ActorSystem()
        {
            SystemCount = 0;
        }
        
        internal ActorSystem(string name)
        {
            Name      = ActorPath.Sanitize(name);
            Location  = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors    = new Dictionary<ActorPath, ActorContainer>();
            Scheduler = new Scheduler(this);
            
            Root = new RootSupervisor(this);
            Root.Populate(this, ActorReferences.Nobody, Location);
            var rootcontainer = new ActorContainer(Root);
            Actors.Add(Location, rootcontainer);
            Root.CallOnCreate();
            
            Log = new ActorLogger(Root);
            
            Lost = new LostLetters(this);
            Lost.Populate(this, new LocalActorReference(this, Root.Path), new ActorPath(Location.Path, "lost-letters"));
            var lostcontainer = new ActorContainer(Lost);
            Actors.Add(Lost.Path, lostcontainer);
            Lost.CallOnCreate();
            
            Scheduler.Start();
            
            SystemCount++;
        }
        
        public static ActorSystem Create(string name)
        {
            return new ActorSystem(name);
        }
        
        public void WaitForSystemShutdown()
        {
            throw new NotImplementedException();
        }
        
        public void WaitForSystemShutdown(CancellationTokenSource cancel_token)
        {
            throw new NotImplementedException();
        }
        
        public IActorReference NewActor(ActorSchematic schematic, string name)
        {
            var child = CreateChildActor(Root, schematic, name);
            return child;
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
                var actor = Actors[path];
                
                actor.Status = ActorStatus.Destroying;
                
                actor.Actor.CallOnDestruction();
                
                actor.Actor.DestroyHandlerStack();
                
                if (Actors.ContainsKey(actor.Actor.InternalParent.Path))
                {
                    // remove self from parent
                    var parent = Actors[actor.Actor.InternalParent.Path];
                    var child_to_remove = parent.Actor.InternalChildren.Find(x => x.Path == path);
                    if (child_to_remove is { })
                    {
                        parent.Actor.InternalChildren.Remove(child_to_remove);
                    }
                }
                
                // recursively destroy children
                foreach (var child in actor.Actor.InternalChildren)
                {
                    var cpath = child.Path;
                    DestroyActor(path);
                }
                
                actor.Status = ActorStatus.Destroyed;
                
                Actors.Remove(path);
            }
        }
        
        public void DestroyActor(string path) => DestroyActor(new ActorPath(path));
        
        public void Shutdown()
        {
            Scheduler.Stop();
            throw new NotImplementedException();
        }
        
        internal ActorContainer? GetActor(ActorPath path)
        {
            if (Actors.ContainsKey(path))
            {
                return Actors[path];
            }
            return null;
        }
        
        internal void EnqueueForMessageProcessing<T>(ActorPath receiver, T data, IActorReference sender)
        {
            if (receiver.IsRemote)
            {
                AskNetwork(receiver, data, sender);
            }
            else if (receiver.IsLocal && Actors.ContainsKey(receiver))
            {
                var token = new ActorQueueToken(this, receiver, typeof(T), (object)data!, sender);
                Scheduler.Enqueue(token);
            }
            else
            {
                Unhandled(sender, receiver, data!);
            }
        }
        
        internal void AskNetwork<T>(ActorPath receiver, T data, IActorReference sender)
        {
            // TODO: check the network if it can handle the message. If not, treat it as
            // unhandled both here and locally.
            // TODO: before even bothering to ask the network, check if the actor path
            // is a foreign one or not.
            // We shouldn't hardcode this, it should be modular. Like I think the default
            // should be "caesura.tcp.json://..." but it should be something people can
            // write alternative protocols for.
            
            throw new NotImplementedException();
        }
        
        internal void Unhandled(IActorReference sender, ActorPath receiver, object message)
        {
            var recpath = new LocalActorReference(this, receiver);
            Unhandled(sender, recpath, message);
        }
        
        internal void Unhandled(IActorReference sender, IActorReference receiver, object message)
        {
            var lost = new LostLetter(sender, receiver, message);
            EnqueueForMessageProcessing(Lost.Path, lost, sender);
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
            
            if (actor is null)
            {
                return ActorReferences.Nobody;
            }
            
            // TODO: save schematic for revival
            
            actor.Populate(this, self, path);
            var container = new ActorContainer(actor);
            Actors.Add(actor.Path, container);
            parent.InternalChildren.Add(new LocalActorReference(this, actor.Path));
            actor.CallOnCreate();
            
            return new LocalActorReference(this, actor.Path);
        }
        
        internal void BeginSessionPersistence(Actor actor)
        {
            Scheduler.BeginSessionPersistence(actor);
        }
        
        internal void EndSessionPersistence(Actor actor)
        {
            Scheduler.EndSessionPersistence(actor);
        }
        
        public void Dispose()
        {
            Shutdown();
            SystemCount--;
        }
    }
}
