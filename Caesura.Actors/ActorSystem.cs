
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
    // - Legio - Latin for "legion"
    // - Colony
    // - Zima
    // - Oozei (大勢 / おおぜい) - Many people
    // - Consol - Corruption of "console" to match Solace
    // - Scarlet
    // - Plasma - because the actors are basically the blood cells of my projects
    // - Purazuma - Roumaji of the katakana of "plasma" (プラズマ)
    // - Kessho / Kesshou (血漿 / けっしょう) - Blood plasma
    //
    // Currently considering Purazuma the most
    //
    // TODO: persistance handling, by saving the user's
    // IStateSerializeHandler/IStateDeserializeHandler objects.
    // Have the user plug in some serialization handler for persisting/restoring data.
    // TODO: this could use some refactoring, it's a bit of a god class
    
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
        internal CancellationTokenSource CancelToken { get; set; }
        
        internal RootSupervisor Root { get; set; }
        internal LostLetters Lost { get; set; }
        
        static ActorSystem()
        {
            SystemCount = 0;
        }
        
        internal ActorSystem(string name, ActorsConfiguration? config)
        {
            Config      = config ?? ActorsConfiguration.CreateDefault();
            
            Name        = ActorPath.Sanitize(name);
            Location    = new ActorPath($"{ActorPath.ProtocolName}://{Name}/");
            Actors      = new Dictionary<ActorPath, ActorContainer>();
            CancelToken = new CancellationTokenSource();
            Scheduler   = new Scheduler();
            
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
            WaitForSystemShutdown(CancelToken.Token);
        }
        
        public void WaitForSystemShutdown(CancellationToken cancel_token)
        {
            while (!CancelToken.IsCancellationRequested && !cancel_token.IsCancellationRequested)
            {
                Thread.Sleep(15);
            }
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
                    Log.Warning($"Tried to destroy {path} but it is not in the system");
                    return;
                }
                
                Log.Info($"Destroying actor {path}");
                
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
            if (!CancelToken.IsCancellationRequested)
            {
                Log.Info($"Shutting down actor system {Location} ...");
                CancelToken.CancelAfter(4900); // TODO: play with this
                Scheduler.Stop();
                SystemCount--;
            }
            else
            {
                Log.Warning($"Attempt to shut down system when it is not running");
            }
        }
        
        internal ActorContainer? GetActor(ActorPath path)
        {
            if (path.IsRemote)
            {
                Log.Warning($"Tried to get actor {path} but it is remote");
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
                    else // lost letter
                    {
                        var lost = GetActor(Lost.Path);
                        var letter = new LostLetter(sender, receiver, (object)data!);
                        var token = new ActorQueueToken(this, receiver, typeof(LostLetter), (object)letter!, sender);
                        Scheduler.Enqueue(lost!, token);
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
            var lost = new LostLetter(sender, receiver.Path, message);
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
                    Log.Debug($"Attempted to create actor that already exists {path}");
                    throw new InvalidOperationException($"Actor with the path {path} already exists");
                }
                
                var self = GetReference(parent.Path);
                var actor = child.Create(this, parent.Path, path);
                
                if (actor is null)
                {
                    return ActorReferences.Nobody;
                }
            
                actor.Populate(this, self, path);
                var container = new ActorContainer(child, child_name, actor);
                Actors.Add(actor.Path, container);
                var reference = GetReference(actor.Path);
                parent.InternalChildren.Add(reference);
                
                Log.Info($"Created actor {path}");
                
                var result = actor.CallOnCreate();
                
                if (result.ProcessingResult == ActorProcessingResult.Errored)
                {
                    InformUnhandledError(parent.Path, reference, result.Exception!);
                }
                
                return reference;
            }
        }
        
        internal void DestroyFaultedActor(ActorPath receiver, IActorReference faulted_actor)
        {
            // TODO: handle remote
            lock (ActorLock)
            {
                var receiver_container = GetActor(receiver);
                var faulted_container  = GetActor(faulted_actor.Path);
                
                if (receiver_container is null || faulted_container is null)
                {
                    if (receiver_container is null && faulted_container is null)
                    {
                        Log.Warning($"Both the receiver and the faulted actor are not in the system");
                    }
                    else if (receiver_container is null)
                    {
                        Log.Warning($"The receiver is not in the system");
                    }
                    else
                    {
                        Log.Warning($"The faulted actor is not in the system");
                    }
                    return;
                }
                
                Log.Info($"Destroying faulted actor {faulted_actor.Path}");
                
                faulted_container.Actor.CallOnDestruction();
                
                receiver_container.Actor.InternalChildren.Remove(faulted_actor);
                Actors.Remove(faulted_actor.Path);
                Scheduler.InformActorDestruction(faulted_container);
            }
        }
        
        internal void RestartFaultedActor(ActorPath receiver, IActorReference faulted_actor)
        {
            // TODO: handle collecting the state and passing it back
            // TODO: handle remote
            lock (ActorLock)
            {
                var receiver_container = GetActor(receiver);
                var faulted_container  = GetActor(faulted_actor.Path);
                
                if (receiver_container is null || faulted_container is null)
                {
                    if (receiver_container is null && faulted_container is null)
                    {
                        Log.Warning($"Both the receiver and the faulted actor are not in the system");
                    }
                    else if (receiver_container is null)
                    {
                        Log.Warning($"The receiver is not in the system");
                    }
                    else
                    {
                        Log.Warning($"The faulted actor is not in the system");
                    }
                    return;
                }
                
                var parent_path = GetReference(receiver_container.Actor.Path);
                
                var pre_reload_result = faulted_container.Actor.CallPreReload();
                if (pre_reload_result.ProcessingResult == ActorProcessingResult.Errored)
                {
                    InformUnhandledError(receiver_container.Actor.InternalParent.Path, parent_path, pre_reload_result.Exception!);
                    return;
                }
                
                var child = faulted_container.Schematic.Create(this, receiver, faulted_actor.Path);
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
                
                child.Populate(this, parent_path, faulted_container.Actor.Path);
                
                faulted_container.Actor = child;
                faulted_container.Unfault();
                
                var on_create_result = child.CallOnCreate();
                if (on_create_result.ProcessingResult == ActorProcessingResult.Errored)
                {
                    InformUnhandledError(receiver_container.Actor.InternalParent.Path, parent_path, on_create_result.Exception!);
                    return;
                }
                
                var post_reload_result = child.CallPostReload();
                if (post_reload_result.ProcessingResult == ActorProcessingResult.Errored)
                {
                    InformUnhandledError(receiver_container.Actor.InternalParent.Path, parent_path, post_reload_result.Exception!);
                    return;
                }
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
                Log.Warning($"Tried to fault actor {path} but it is not in the system");
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
            if (!CancelToken.IsCancellationRequested)
            {
                Shutdown();
            }
        }
    }
}
