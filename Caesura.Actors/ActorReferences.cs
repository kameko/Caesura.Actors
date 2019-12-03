
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
            NoSender = new NoActorReference("nosender");
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
            
            public void Ask<T>(T data, IActorReference sender, Action<T> continue_with)
            {
                // Do nothing.
            }
            
            public void Ask<T>(T data, IActorReference sender, Action<T> continue_with, TimeSpan timeout)
            {
                // Do nothing.
            }
            
            public R Ask<T, R>(T data, IActorReference sender, Func<T, R> continue_with)
            {
                // Do nothing.
                return continue_with.Invoke(data);
            }
            
            public R Ask<T, R>(T data, IActorReference sender, Func<T, R> continue_with, TimeSpan timeout)
            {
                // Do nothing.
                return continue_with.Invoke(data);
            }
            
            public void InformUnhandledError(IActorReference sender, Exception e)
            {
                // Do nothing.
            }
            
            public void Destroy()
            {
                // Do nothing.
            }
        }
    }
}
