
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
        // TODO: create TcpListener to listen for clients and then create a TcpClient and connect
        // to all servers in the RemoteNodes collection in the system config
        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=netcore-3.0 
        
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
