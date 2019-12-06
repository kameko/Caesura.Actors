
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorQueueToken
    {
        public ActorSystem System { get; private set; }
        public ActorPath Receiver { get; private set; }
        public Type DataType { get; private set; }
        public object Data { get; private set; }
        public IActorReference Sender { get; private set; }
        
        public ActorQueueToken(ActorSystem system, ActorPath receiver, Type data_type, object data, IActorReference sender)
        {
            System   = system;
            Receiver = receiver;
            DataType = data_type;
            Data     = data;
            Sender   = sender;
        }
    }
}
