
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class RootSupervisor : Actor
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
                    $"System supervisor encountered an unhandled exception." +
                    $"Actor system will now be shut down."
                );
                
                fault.Destroy();
                
                System.Shutdown();
            };
            
            var on_any = HandleAny.Create(this);
            on_any += msg =>
            {
                
            };
        }
        
        internal override void InformParentOfError(Exception e)
        {
            // TODO: I dunno, send self a Fault message?
        }
    }
}
