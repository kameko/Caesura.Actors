
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class MessageStash
    {
        private Actor Owner { get; set; }
        
        public MessageStash(Actor owner)
        {
            Owner = owner;
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
