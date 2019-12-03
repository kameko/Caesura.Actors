
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class ActorPath
    {
        public static string ProtocolName => "caesura";
        
        public string Path { get; private set; }
        public string Name => GetName();
        public string Location => GetLocation();
        
        public ActorPath(string path)
        {
            Path = path;
        }
        
        public ActorPath(string path, string name)
        {
            path = path.TrimEnd('/');
            name = Sanitize(name);
            Path = $"{path}/{name}";
        }
        
        internal static string Sanitize(string path)
        {
            string sanitized = new string(path.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            return sanitized;
        }
        
        private string GetName()
        {
            if (Path.EndsWith('/') && Path.Count(c => c == '/') == 3)
            {
                // this is the root actor (path is "caesura://something/")
                return "/";
            }
            if (Path.EndsWith('/'))
            {
                return string.Empty;
            }
            var paths = Path.Split('/');
            return paths.Last();
        }
        
        private string GetLocation()
        {
            var path = Path.Substring(0, Path.Length - Name.Length);
            return path;
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
