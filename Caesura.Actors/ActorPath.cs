
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
        
        public ActorPath(string path, string name)
        {
            Path = $"{path}/{name}";
        }
        
        private string GetName()
        {
            var paths = Path.Split('/');
            return paths.Last();
        }
        
        public static bool operator == (ActorPath x, ActorPath y)
        {
            return string.Equals(x.Path, y.Path, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool operator != (ActorPath x, ActorPath y)
        {
            return !(x == y);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is ActorPath other)
            {
                return this == other;
            }
            return false;
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
