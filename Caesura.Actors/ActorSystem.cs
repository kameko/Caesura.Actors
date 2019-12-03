
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
    
    public class ActorSystem
    {
        public string Name { get; private set; }
        public ActorPath Location { get; private set; }
        private Dictionary<ActorPath, Actor> Actors { get; set; }
        private List<ActorQueueToken> ActorQueue { get; set; }
        
        private RootSupervisor Root { get; set; }
        private LostLetters Lost { get; set; }
        
        internal ActorSystem(string name)
        {
            Name       = ActorPath.Sanitize(name);
            Location   = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors     = new Dictionary<ActorPath, Actor>();
            ActorQueue = new List<ActorQueueToken>();
            
            Root = new RootSupervisor(this);
            Root.Populate(this, ActorReferences.Nobody, Location);
            Actors.Add(Location, Root);
            
            Lost = new LostLetters(this);
            Lost.Populate(this, new LocalActorReference(this, Root.Path), new ActorPath(Location.Path, "lost-letters"));
            Actors.Add(Lost.Path, Lost);
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
            Actors.Add(actor.Path, actor);
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
}
