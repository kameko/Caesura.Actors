
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    
    
    
    internal class Scheduler
    {
        internal ActorSystem System { get; set; }
        private List<ActorQueueToken> Queue { get; set; }
        private List<Actor> PersistedActors { get; set; }
        
        public Scheduler(ActorSystem system)
        {
            System          = system;
            Queue           = new List<ActorQueueToken>();
            PersistedActors = new List<Actor>();
        }
        
        // TODO: use Task.Run
        // Thread.CurrentThread.Name = actor.Path.Path
        
        internal void BeginSessionPersistence(Actor actor)
        {
            if (PersistedActors.Contains(actor))
            {
                throw new InvalidOperationException($"Actor {actor.Path} is already having it's session persisted");
            }
            
            PersistedActors.Add(actor);
        }
        
        internal void EndSessionPersistence(Actor actor)
        {
            if (!PersistedActors.Contains(actor))
            {
                throw new InvalidOperationException($"Actor {actor.Path} has not been persisted");
            }
            
            PersistedActors.Remove(actor);
        }
    }
}
