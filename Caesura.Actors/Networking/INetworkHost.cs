
namespace Caesura.Actors.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    
    public interface INetworkHost : IDisposable
    {
        void AcceptSystem(ActorSystem system);
    }
}
