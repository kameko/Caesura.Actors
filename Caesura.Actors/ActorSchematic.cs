
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
        
        internal Actor? Create(ActorSystem system, ActorPath path)
        {
            try
            {
                var actor = Factory.Invoke();
                return actor;
            }
            catch (Exception e)
            {
                system.Log.Warning(e, $"Factory could not instance actor {path}");
                return null;
            }
        }
    }
}
