
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    /// <summary>
    /// An actor being sent a Halt message will cause it to be
    /// immediately and forcibly shut down.
    /// </summary>
    public class Halt
    {
        private static Halt _instance = new Halt();
        public static Halt Instance => _instance;
        
        public Halt()
        {
            
        }
    }
}
