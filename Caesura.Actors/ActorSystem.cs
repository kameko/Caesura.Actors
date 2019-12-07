
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    
    // TODO: consider renaming. possible names:
    // - Horizon
    // - Sundry (means "many")
    // - Legion
    // - Oozei (大勢 / おおぜい) - Many people
    // - Consol - Corruption of "console" to match Solace
    // - Scarlet
    //
    // TODO: persistance handling, by saving the user's
    // IStateSerializeHandler/IStateDeserializeHandler
    // objects
    // TODO: config, including allowing a custom scheduler.
    // TODO: Actors isn't thread-safe, look into that.
    
    public class ActorSystem : IDisposable
    {
        internal static int SystemCount { get; set; }
        
        private readonly object ActorLock = new object();
        public string Name { get; private set; }
        public ActorPath Location { get; private set; }
        private Dictionary<ActorPath, ActorContainer> Actors { get; set; }
        private IScheduler Scheduler { get; set; }
        internal ActorLogger Log { get; set; }
        internal ActorsConfiguration Config { get; private set; }
        
        internal RootSupervisor Root { get; set; }
        internal LostLetters Lost { get; set; }
        
        static ActorSystem()
        {
            SystemCount = 0;
        }
        
        internal ActorSystem(string name, ActorsConfiguration? config)
        {
            Config    = config ?? ActorsConfiguration.CreateDefault();
            
            Name      = ActorPath.Sanitize(name);
            Location  = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors    = new Dictionary<ActorPath, ActorContainer>();
            Scheduler = new Scheduler();
            
            Scheduler.AssignSystem(this);
            
            Root = new RootSupervisor(this);
            Root.Populate(this, ActorReferences.Nobody, Location);
            var rootcontainer = new ActorContainer(new ActorSchematic(() => new RootSupervisor(this)), Location.Name, Root);
            Actors.Add(Location, rootcontainer);
            Root.CallOnCreate();
            
            Log = new ActorLogger(Root);
            
            Lost = new LostLetters(this);
            Lost.Populate(this, new LocalActorReference(this, Root.Path), new ActorPath(Location.Path, "lost-letters"));
            var lostcontainer = new ActorContainer(new ActorSchematic(() => new LostLetters(this)), "lost-letters", Lost);
            Actors.Add(Lost.Path, lostcontainer);
            Lost.CallOnCreate();
            
            if (!Config.ManuallyStartScheduler)
            {
                Scheduler.Start();
            }
            
            SystemCount++;
        }
        
        internal ActorSystem(string name) : this(name, null)
        {
            
        }
        
        public static ActorSystem Create(string name)
        {
            return new ActorSystem(name);
        }
        
        public static ActorSystem Create(string name, ActorsConfiguration config)
        {
            return new ActorSystem(name, config);
        }
        
        /// <summary>
        /// Only call if ActorsConfiguration.ManuallyStartScheduler is set to true,
        /// otherwise this is called automatically.
        /// </summary>
        public void Start()
        {
            Scheduler.Start();
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
            DestroyActor(reference.Path);
        }
        
        public void DestroyActor(ActorPath path)
        {
            // TODO: handle destroying remote actor
            
            lock (ActorLock)
            {
                var container = GetActor(path);
                
                if (container is null)
                {
                    // TODO: log
                    return;
                }
                
                container.Status = ActorStatus.Destroying;
                
                container.Actor.CallOnDestruction();
                
                container.Actor.DestroyHandlerStack();
                
                if (Actors.ContainsKey(container.Actor.InternalParent.Path))
                {
                    // remove self from parent
                    var parent = Actors[container.Actor.InternalParent.Path];
                    var child_to_remove = parent.Actor.InternalChildren.Find(x => x.Path == path);
                    if (child_to_remove is { })
                    {
                        parent.Actor.InternalChildren.Remove(child_to_remove);
                    }
                }
                
                DestroyChildren(container);
                
                container.Status = ActorStatus.Destroyed;
                
                Actors.Remove(path);
                Scheduler.InformActorDestruction(container);
            }
        }
        
        public void DestroyActor(string path) => DestroyActor(new ActorPath(path));
        
        public void Shutdown()
        {
            Log.Info($"Shutting down actor system {Location} ...");
            Scheduler.Stop();
        }
        
        internal ActorContainer? GetActor(ActorPath path)
        {
            if (path.IsRemote)
            {
                // TODO: log, maybe return a failure?
                return null;
            }
            lock (ActorLock)
            {
                if (Actors.ContainsKey(path))
                {
                    return Actors[path];
                }
            }
            return null;
        }
        
        internal IActorReference GetReference(ActorPath path)
        {
            // TODO: handle remote
            var reference = new LocalActorReference(this, path);
            return reference;
        }
        
        internal void EnqueueForMessageProcessing<T>(ActorPath receiver, T data, IActorReference sender)
        {
            if (receiver.IsRemote)
            {
                AskNetwork(receiver, data, sender);
            }
            else if (receiver.IsLocal)
            {
                lock (ActorLock)
                {
                    var container = GetActor(receiver);
                    if (!(container is null))
                    {
                        var token = new ActorQueueToken(this, receiver, typeof(T), (object)data!, sender);
                        Scheduler.Enqueue(container, token);
                    }
                }
            }
            else
            {
                Unhandled(sender, receiver, data!);
            }
        }
        
        internal void AskNetwork<T>(ActorPath receiver, T data, IActorReference sender)
        {
            // TODO: check the network if it can handle the message. If not, treat it as
            // unhandled both on the network and locally.
            // We shouldn't hardcode this, it should be modular. Like I think the default
            // should be "caesura.tcp.json://..." but it should be something people can
            // write alternative protocols for.
            
            throw new NotImplementedException();
        }
        
        internal void Unhandled(IActorReference sender, ActorPath receiver, object message)
        {
            var recpath = GetReference(receiver);
            Unhandled(sender, recpath, message);
        }
        
        internal void Unhandled(IActorReference sender, IActorReference receiver, object message)
        {
            var lost = new LostLetter(sender, receiver, message);
            EnqueueForMessageProcessing(Lost.Path, lost, sender);
        }
        
        internal void InformUnhandledError(ActorPath receiver, IActorReference faulted_actor, Exception e)
        {
            var msg = new Fault(this, receiver, faulted_actor, e);
            EnqueueForMessageProcessing(receiver, msg, faulted_actor);
        }
        
        internal IActorReference CreateChildActor(Actor parent, ActorSchematic child, string child_name)
        {
            var path = new ActorPath(parent.Path.Path, child_name);
            
            lock (ActorLock)
            {
                if (Actors.ContainsKey(path))
                {
                    throw new InvalidOperationException($"Actor with the path {path} already exists");
                }
                
                var self = GetReference(parent.Path);
                var actor = child.Create(this);
                
                if (actor is null)
                {
                    return ActorReferences.Nobody;
                }
            
                actor.Populate(this, self, path);
                var container = new ActorContainer(child, child_name, actor);
                Actors.Add(actor.Path, container);
                var reference = GetReference(actor.Path);
                parent.InternalChildren.Add(reference);
                actor.CallOnCreate();
                
                return reference;
            }
        }
        
        internal void DestroyFaultedActor(ActorPath receiver, IActorReference faulted_actor)
        {
            lock (ActorLock)
            {
                var receiver_container = GetActor(receiver);
                var faulted_container  = GetActor(faulted_actor.Path);
                
                if (receiver_container is null || faulted_container is null)
                {
                    // TODO: log
                    return;
                }
                
                // TODO: log
                
                faulted_container.Actor.CallOnDestruction();
                
                receiver_container.Actor.InternalChildren.Remove(faulted_actor);
                Actors.Remove(faulted_actor.Path);
                Scheduler.InformActorDestruction(faulted_container);
            }
        }
        
        // TODO: handle collecting the state and passing it back
        internal void RestartFaultedActor(ActorPath receiver, IActorReference faulted_actor)
        {
            lock (ActorLock)
            {
                var receiver_container = GetActor(receiver);
                var faulted_container  = GetActor(faulted_actor.Path);
                
                if (receiver_container is null || faulted_container is null)
                {
                    // TODO: log
                    return;
                }
                
                faulted_container.Actor.CallPreReload();
                
                var child = faulted_container.Schematic.Create(this);
                if (child is null)
                {
                    Log.Warning($"Attempted to re-create actor of {faulted_actor.Path} but it failed");
                    
                    Actors.Remove(faulted_container.Actor.Path);
                    Scheduler.InformActorDestruction(faulted_container);
                    
                    var faulted_child_path = receiver_container.Actor.InternalChildren.Find(x => x.Path == faulted_container.Actor.Path);
                    if (!(faulted_child_path is null))
                    {
                        // faulted's children are already destroyed, so we don't need to worry about them.
                        receiver_container.Actor.InternalChildren.Remove(faulted_child_path);
                    }
                    return;
                }
                
                var parent_path = GetReference(receiver_container.Actor.Path);
                child.Populate(this, parent_path, faulted_container.Actor.Path);
                
                faulted_container.Actor = child;
                faulted_container.Unfault();
                
                child.CallOnCreate();
                child.CallPostReload();
            }
        }
        
        internal void FaultedActor(Actor actor, Exception e)
        {
            FaultedActor(actor.Path, e);
        }
        
        private void FaultedActor(ActorPath path, Exception e)
        {
            var container = GetActor(path);
            
            if (container is null)
            {
                // TODO: log
                return;
            }
            
            var reference = GetReference(container.Actor.Path);
            container.Actor.InternalParent.InformError(reference, e);
            container.SetFault(e);
            
            container.Actor.CallOnDestruction();
            DestroyChildren(container);
        }
        
        private void DestroyChildren(ActorContainer container)
        {
            // recursively destroy children
            var children = new List<IActorReference>(container.Actor.InternalChildren);
            foreach (var child in children)
            {
                DestroyActor(child.Path);
            }
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
