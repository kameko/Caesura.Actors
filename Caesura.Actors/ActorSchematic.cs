
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorSchematic
    {
        private Func<Actor> Factory { get; set; }
        
        public ActorSchematic(Func<Actor> factory)
        {
            Factory = factory;
        }
        
        internal Actor? Create(ActorSystem system)
        {
            try
            {
                var actor = Factory.Invoke();
                return actor;
            }
            catch
            {
                // TODO: log error.
                return null;
            }
        }
    }
}
