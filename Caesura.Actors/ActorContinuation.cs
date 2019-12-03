
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    // Callbacks from actors using Wait, Ask, or anything
    // else with a "continue_with" callback should exclusively
    // use this type presented to them in the callback's arguments.
    // Attempting to access the current actor's standard properties
    // such as Sender or CurrentMessage may fail or have unexpected
    // results.
    
    // TODO: consider if an actor really needs to use this or if
    // we be more clever with callbacks and how to continue them.
    // I think instead an actor should have some kind of internal
    // InContinuation property to block them from getting new messages
    // until the actor has finished processing all of it's continuations.
    
    public class ActorContinuation
    {
        
        
        public ActorContinuation()
        {
            
        }
        
        public static ActorContinuation NoContinuation()
        {
            // TODO:
            return new ActorContinuation();
        }
    }
}
