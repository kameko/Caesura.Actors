
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
    
    public class ActorSystem
    {
        public string Name { get; private set; }
        public ActorPath Location { get; private set; }
        private Dictionary<ActorPath, ActorContainer> Actors { get; set; }
        private List<ActorQueueToken> ActorQueue { get; set; }
        private Scheduler Scheduler { get; set; }
        private ActorLogger Log { get; set; }
        
        private RootSupervisor Root { get; set; }
        private LostLetters Lost { get; set; }
        
        internal ActorSystem(string name)
        {
            Name       = ActorPath.Sanitize(name);
            Location   = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors     = new Dictionary<ActorPath, ActorContainer>();
            ActorQueue = new List<ActorQueueToken>();
            Scheduler  = new Scheduler(this);
            
            Root = new RootSupervisor(this);
            Root.Populate(this, ActorReferences.Nobody, Location);
            var rootcontainer = new ActorContainer(Root);
            Actors.Add(Location, rootcontainer);
            
            Log = new ActorLogger(Root);
            
            Lost = new LostLetters(this);
            Lost.Populate(this, new LocalActorReference(this, Root.Path), new ActorPath(Location.Path, "lost-letters"));
            var lostcontainer = new ActorContainer(Lost);
            Actors.Add(Lost.Path, lostcontainer);
        }
        
        public static ActorSystem Create(string name)
        {
            // TODO: have a static ActorSystem instance counter, and have each
            // scheduler use less threads the higher the counter is. So on a 4
            // core machine, one instance would have a scheduler with 4 threads,
            // then instancing a new one would cause the new system to instance with
            // 2 threads, then the first instance would gracefully stop two of it's
            // threads to evenly split the threads between the system. After there are
            // more instances than threads, just have each instance run with one thread.
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
                
                actor.Actor.DestroyCellStack();
                
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
        
        internal void EnqueueWait(Actor actor, TimeSpan time, Action<ActorContinuation> continue_with)
        {
            // TODO: have this send a special internal message type that will get
            // sent back to the actor and run it's continue_with inside of the
            // actor in a special internal Receive method.
            // We'll send the message back to the actor after the timespan ends, which
            // will be enqueued in a custom queue system.
            // Don't forget to save the current message somewhere so the actor doesn't
            // see the new internal message. Maybe have the internal message hold it.
            
            throw new NotImplementedException();
        }
        
        internal void EnqueueForMessageProcessing<T>(ActorPath receiver, T data, IActorReference sender)
        {
            if (!Actors.ContainsKey(receiver))
            {
                // TODO: ask the network if they have this actor, otherwise...
                var recpath = new LocalActorReference(this, receiver);
                Unhandled(sender, recpath, (object)data!);
                return;
            }
            
            var token = new ActorQueueToken(this, receiver, typeof(T), (object)data!, sender);
            ActorQueue.Add(token);
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
            return new LocalActorReference(this, actor.Path);
        }
        
        internal void BeginSessionPersistence(Actor actor)
        {
            // TODO: tell the scheduler we're not done processing.
            throw new NotImplementedException();
        }
        
        internal void EndSessionPersistence(Actor actor)
        {
            // TODO: tell the scheduler we're ready for a new message.
            throw new NotImplementedException();
        }
    }
    
    internal enum ActorStatus
    {
        Ready,
        Enqueued,
        Destroying,
        Destroyed,
    }
    
    internal class ActorContainer
    {
        public Actor Actor { get; set; }
        public ActorStatus Status { get; set; }
        
        public ActorContainer(Actor actor)
        {
            Status = ActorStatus.Ready;
            Actor = actor;
        }
    }
}
