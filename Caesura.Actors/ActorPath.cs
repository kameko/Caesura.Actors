
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
        public string Protocol => GetProtocol();
        public string ProtocolExtension => GetProtocolExtension();
        public bool IsLocal => string.IsNullOrEmpty(ProtocolExtension);
        public bool IsRemote => !string.IsNullOrEmpty(ProtocolExtension);
        
        private string? NameCache;
        private string? LocationCache;
        private string? ProtocolCache;
        private string? ProtocolExtensionCache;
        
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
            if (!string.IsNullOrEmpty(NameCache))
            {
                return NameCache;
            }
            if (NameCache == "@NOT AN ACTOR")
            {
                return string.Empty;
            }
            
            if (Path.EndsWith('/') && Path.Count(c => c == '/') == 3)
            {
                // this is the root actor (path is "caesura://something/")
                var name = "/";
                NameCache = name;
                return name;
            }
            if (Path.EndsWith('/'))
            {
                NameCache = "@NOT AN ACTOR";
                return string.Empty;
            }
            
            var paths = Path.Split('/');
            var path = paths.Last();
            NameCache = path;
            return path;
        }
        
        private string GetLocation()
        {
            if (!string.IsNullOrEmpty(LocationCache))
            {
                return LocationCache;
            }
            
            var path = Path.Substring(0, Path.Length - Name.Length);
            LocationCache = path;
            return path;
        }
        
        private string GetProtocol()
        {
            if (!string.IsNullOrEmpty(ProtocolCache))
            {
                return ProtocolCache;
            }
            
            var protocols = Path.Split("://");
            var protocol = protocols[0];
            ProtocolCache = protocol;
            return protocol;
        }
        
        private string GetProtocolExtension()
        {
            if (!string.IsNullOrEmpty(ProtocolExtensionCache))
            {
                return ProtocolExtensionCache;
            }
            
            var protocol = GetProtocol();
            if (!protocol.Contains('.'))
            {
                return string.Empty;
            }
            
            var extension = protocol.Substring(ProtocolName.Length);
            if (extension.StartsWith('.'))
            {
                extension = extension.TrimStart('.');
            }
            ProtocolExtensionCache = extension;
            return extension;
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
