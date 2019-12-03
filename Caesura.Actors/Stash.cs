
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class Stash
    {
        private Actor Owner { get; set; }
        
        public Stash(Actor owner)
        {
            Owner = owner;
        }
        
        public void StashMessage()
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
