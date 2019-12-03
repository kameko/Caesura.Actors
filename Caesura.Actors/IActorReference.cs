
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface IActorReference : ITellable
    {
        ActorPath Path { get; }
        void Tell<T>(T data, IActorReference sender);
        void Ask<T>(T data, IActorReference sender, Action continueWith);
        void Ask<T>(T data, IActorReference sender, Action continueWith, TimeSpan timeout);
        R Ask<T, R>(T data, IActorReference sender, Action<R> continueWith);
        R Ask<T, R>(T data, IActorReference sender, Action<R> continueWith, TimeSpan timeout);
        void InformUnhandledError(Exception e);
    }
}
