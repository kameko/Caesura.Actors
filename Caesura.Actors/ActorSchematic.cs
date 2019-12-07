
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
        
        internal Actor? Create(ActorSystem system, ActorPath parent_path, ActorPath path)
        {
            try
            {
                var actor = Factory.Invoke();
                return actor;
            }
            catch (Exception e)
            {
                system.Log.Warning(e, $"Factory could not instance actor {path}");
                var parent = system.GetReference(parent_path);
                if (parent is null)
                {
                    system.Log.Warning($"Attempted to warn parent of faulted schematic for {path} but could not find it");
                }
                else
                {
                    parent.InformError(parent, e);
                }
                return null;
            }
        }
    }
}
