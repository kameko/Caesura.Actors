
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorPath
    {
        public string Path { get; private set; }
        public string Name => GetName();
        
        public ActorPath(string path)
        {
            Path = path;
        }
        
        private string GetName()
        {
            var paths = Path.Split('/');
            return paths.Last();
        }
        
        public override string ToString()
        {
            return Path;
        }
        
        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
