
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    
    public abstract class Actor : ITellable
    {
        public ActorPath Path { get; private set; }
        private ActorSystem System { get; set; }
        protected ActorLogger ActorLog { get; private set; }
        protected IActorReference Sender => InternalSender;
        protected IActorReference Self { get; private set; }
        protected IActorReference Parent => InternalParent;
        protected IReadOnlyList<IActorReference> Children => InternalChildren;
        protected MessageStash Stash { get; private set; }
        
        internal bool HasFaulted { get; set; }
        internal HandlerService Handlers { get; set; }
        internal IActorReference InternalSender { get; set; }
        internal IActorReference InternalParent { get; set; }
        internal List<IActorReference> InternalChildren { get; set; }
        internal ActorLogger InternalLog { get; set; }
        
        public Actor()
        {
            Path             = null!;
            ActorLog         = null!;
            System           = null!;
            InternalSender   = null!;
            Self             = null!;
            Stash            = null!;
            
            Handlers         = null!;
            InternalParent   = null!;
            InternalChildren = null!;
            InternalLog      = null!;
        }
        
        internal void Populate(ActorSystem system, IActorReference parent, ActorPath path)
        {
            Path             = path;
            ActorLog         = new ActorLogger(this);
            System           = system;
            Self             = new LocalActorReference(system, path);
            Stash            = new MessageStash(system, this);
            
            Handlers         = new HandlerService(system, this);
            InternalParent   = parent;
            InternalChildren = new List<IActorReference>();
            InternalLog      = ActorLog;
        }
        
        protected virtual void PreReload()
        {
            
        }
        
        protected virtual void PostReload()
        {
            
        }
        
        protected virtual void OnDestruction()
        {
            
        }
        
        protected virtual void OnCreate()
        {
            
        }
        
        public virtual IState Snapshot()
        {
            return default!;
        }
        
        public virtual void LoadState(IState state)
        {
            
        }
        
        // return Success if it didn't error so we can call PostReload,
        // otherwise kill the actor.
        internal ActorResult CallPreReload()
        {
            try
            {
                PreReload();
                return new ActorResult(ActorProcessingResult.Success);
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(PreReload)}");
                return new ActorResult(ActorProcessingResult.Errored, e);
            }
        }
        
        // If this returns Errored, we destroy the actor. Otherwise
        // it would be an endless loop of erroring.
        internal ActorResult CallPostReload()
        {
            try
            {
                PostReload();
                return new ActorResult(ActorProcessingResult.Success);
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(PostReload)}");
                return new ActorResult(ActorProcessingResult.Errored, e);
            }
        }
        
        // We don't care if this works or not because the actor is
        // getting destroyed anyway.
        internal void CallOnDestruction()
        {
            try
            {
                OnDestruction();
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(OnDestruction)}");
            }
        }
        
        internal ActorResult CallOnCreate()
        {
            try
            {
                OnCreate();
                return new ActorResult(ActorProcessingResult.Success);
            }
            catch (Exception e)
            {
                ActorLog.Verbose(e, $"Error calling {nameof(OnCreate)}");
                return new ActorResult(ActorProcessingResult.Errored, e);
            }
        }
        
        protected void TellChildren<T>(T data)
        {
            if (Children.Count == 0)
            {
                ActorLog.Warning(
                    $"Tried to tell children message of {typeof(T)}, " +
                    $"but no children to tell. Data: {data}"
                );
            }
            
            foreach (var child in Children)
            {
                child.Tell(data, Self);
            }
        }
        
        protected void Tattle<T>(T data)
        {
            Parent.Tell(data, Self);
        }
        
        protected void Respond<T>(T data)
        {
            InternalSender.Tell(data, Self);
        }
        
        protected void Tell<T>(IActorReference actor, T data)
        {
            actor.Tell(data, Self);
        }
        
        protected void Forward<T>(IActorReference actor, T data)
        {
            actor.Tell(data, InternalSender);
        }
        
        protected IActorReference NewChild(ActorSchematic schematic, string name)
        {
            var child = System.CreateChildActor(this, schematic, name);
            return child;
        }
        
        protected void Become(Action method)
        {
            DestroyHandlerStack();
            method.Invoke();
        }
        
        internal void ProcessMessage(IActorReference sender, object message, CancellationToken cancel_token)
        {
            if (System.Config.VerboseLogAllMessages)
            {
                ActorLog.Verbose($"Message sent from {sender}: {{0}}", new object[]{ message });
            }
            
            if (message is Halt)
            {
                DestroySelf();
                return;
            }
            
            if (HasFaulted)
            {
                ActorLog.Warning("Actor cannot process any more messages, it has faulted");
                
                Handlers.CurrentMessage = message;
                Unhandled();
                
                return;
            }
            
            InternalSender = sender;
            var result = Handlers.Handle(message);
            if (result == ActorProcessingResult.Unhandled)
            {
                Unhandled();
            }
        }
        
        protected void Unhandled()
        {
            if (!(Handlers.CurrentMessage is null))
            {
                System.Unhandled(InternalSender, Self, Handlers.CurrentMessage);
            }
        }
        
        protected void DestroySelf()
        {
            System.DestroyActor(Self);
        }
        
        protected bool StrEq(string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }
        
        internal void BeginSessionPersistence()
        {
            System.BeginSessionPersistence(this);
        }
        
        internal void EndSessionPersistence()
        {
            System.EndSessionPersistence(this);
        }
        
        internal virtual void InformParentOfError(Exception e)
        {
            HasFaulted = true;
            System.FaultedActor(this, e);
        }
        
        internal void DestroyHandlerStack()
        {
            Handlers.Clear();
        }
    }
}
