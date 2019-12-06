
namespace Caesura.Actors.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface IScheduler
    {
        void AssignSystem(ActorSystem system);
        void Start();
        void Stop();
        void Enqueue(ActorQueueToken token);
        void BeginSessionPersistence(Actor actor);
        void EndSessionPersistence(Actor actor);
    }
}
