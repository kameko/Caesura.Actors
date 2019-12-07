
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public static class ActorReferences
    {
        public static IActorReference Nobody { get; private set; }
        public static IActorReference NoSender { get; private set; }
        
        static ActorReferences()
        {
            Nobody   = new NoActorReference("nobody");
            NoSender = new NoActorReference("no-sender");
        }
        
        public class NoActorReference : IActorReference
        {
            public ActorPath Path { get; private set; }
            public string Name { get; private set; }
            
            public NoActorReference(string name)
            {
                Path = new ActorPath($"{ActorPath.ProtocolName}://{name}/");
                Name = name;
            }
            
            public void Tell<T>(T data, IActorReference sender)
            {
                // Do nothing.
            }
            
            public void InformError(IActorReference sender, Exception e)
            {
                // Do nothing.
            }
            
            public void Destroy()
            {
                // Do nothing.
            }
            
            public override string ToString()
            {
                return Path.ToString();
            }
        }
    }
}
