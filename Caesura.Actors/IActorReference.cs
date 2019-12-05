
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface IActorReference : ITellable
    {
        ActorPath Path { get; }
        void Tell<T>(T data, IActorReference sender);
        void InformError(IActorReference sender, Exception e);
        void Destroy();
    }
}
