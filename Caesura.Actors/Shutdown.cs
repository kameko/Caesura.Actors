
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    /// <summary>
    /// A friendly, standard shutdown message actors can use
    /// between each other to request a shutdown. Must be manually
    /// handled by the receiver.
    /// </summary>
    public class Shutdown
    {
        public string Reason { get; private set; }
        
        public Shutdown()
        {
            Reason = string.Empty;
        }
        
        public Shutdown(string reason)
        {
            Reason = reason;
        }
    }
}
