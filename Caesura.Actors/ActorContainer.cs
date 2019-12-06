
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal enum ActorStatus
    {
        Ready,
        Enqueued,
        Destroying,
        Destroyed,
    }
    
    internal class ActorContainer
    {
        public string Name { get; set; }
        public Actor Actor { get; set; }
        public ActorStatus Status { get; set; }
        public ActorSchematic Schematic { get; set; }
        public bool Faulted { get; private set; }
        public Exception? Fault { get; set; }
        
        public ActorContainer(ActorSchematic schematic, string name, Actor actor)
        {
            Name      = name;
            Status    = ActorStatus.Ready;
            Schematic = schematic;
            Actor     = actor;
            Faulted   = false;
        }
        
        public void SetFault(Exception e)
        {
            Faulted = true;
            Fault   = e;
        }
        
        public void Unfault()
        {
            Faulted = false;
            Fault   = null;
        }
    }
}
