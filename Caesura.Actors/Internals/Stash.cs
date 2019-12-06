
namespace Caesura.Actors.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class MessageStash
    {
        private ActorSystem System { get; set; }
        private Actor Owner { get; set; }
        
        public MessageStash(ActorSystem system, Actor owner)
        {
            System = system;
            Owner  = owner;
        }
        
        public void Stash()
        {
            throw new NotImplementedException();
        }
        
        public void Unstash()
        {
            throw new NotImplementedException();
        }
        
        public void UnstashAll()
        {
            throw new NotImplementedException();
        }
    }
}
