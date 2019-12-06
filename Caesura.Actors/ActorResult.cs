
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal enum ActorProcessingResult
    {
        Success,
        Errored,
        Unhandled,
    }
    
    internal class ActorResult
    {
        public ActorProcessingResult ProcessingResult { get; set; }
        public Exception? Exception { get; set; }
        
        public ActorResult(ActorProcessingResult result)
        {
            ProcessingResult = result;
        }
        
        public ActorResult(ActorProcessingResult result, Exception e) : this(result)
        {
            Exception = e;
        }
    }
}
