
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface IActorReference : ITellable
    {
        ActorPath Path { get; }
        void Tell<T>(T data, IActorReference sender);
        void Ask<T>(T data, IActorReference sender, Action<T> continue_with);
        void Ask<T>(T data, IActorReference sender, Action<T> continue_with, TimeSpan timeout);
        R Ask<T, R>(T data, IActorReference sender, Func<T, R> continue_with);
        R Ask<T, R>(T data, IActorReference sender, Func<T, R> continue_with, TimeSpan timeout);
        void InformUnhandledError(Exception e);
    }
}
