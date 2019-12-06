
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class RootSupervisor : Actor
    {
        internal ActorSystem System { get; set; }
        
        internal RootSupervisor(ActorSystem system)
        {
            System = system;
        }
        
        protected override void OnCreate()
        {
            Become(Behavior);
        }
        
        protected override void OnDestruction()
        {
            System.Shutdown();
        }
        
        private void Behavior()
        {
            var on_fault = Handler<Fault>.Create(this);
            on_fault += fault =>
            {
                ActorLog.Fatal(
                    fault.Exception,
                    $"System supervisor encountered an unhandled exception. " +
                    $"Actor system will now be shut down."
                );
                
                fault.Destroy();
                
                System.Shutdown();
            };
        }
        
        internal override void InformParentOfError(Exception e)
        {
            if (Children.Count == 0)
            {
                ActorLog.Warning("Tried to inform root supervisor of an error when it has no children to error");
                return;
            }
            var child_ref = Children[0];
            var fault = new Fault(System, Self.Path, child_ref, e);
            
            Self.Tell(fault, child_ref);
        }
    }
}
