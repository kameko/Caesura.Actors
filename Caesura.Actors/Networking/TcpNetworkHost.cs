
namespace Caesura.Actors.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net;
    using System.Net.Sockets;
    
    internal class TcpNetworkHost : INetworkHost
    {
        // TODO: create TcpListener to listen for clients and then create a TcpClient per server
        // in the RemoteNodes collection in the system config
        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=netcore-3.0 
        // TODO: we'll cache the entire networked actor system here for easier, quicker lookup.
        // To ensure consistency, have each node ask the entire system for it's local actor system
        // periodically, default every minute.
        // TODO: have the config choose between compact and readable JSON for IPC
        
        private ActorSystem System { get; set; }
        
        public TcpNetworkHost()
        {
            System = null!;
        }
        
        public void AcceptSystem(ActorSystem system)
        {
            System = system;
        }
        
        public void Dispose()
        {
            
        }
    }
}
