
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface IActorReference : ITellable
    {
        ActorPath Path { get; }
        void Tell<T>(T data);
        void Tell<T>(T data, IActorReference sender);
        Task Ask<T>(T data);
        Task Ask<T>(T data, TimeSpan timeout);
        Task<R> Ask<T, R>(T data);
        Task<R> Ask<T, R>(T data, TimeSpan timeout);
    }
}
