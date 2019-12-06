
namespace Caesura.Actors.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Internals;
    
    public enum ActorStatus
    {
        Ready,
        Enqueued,
        Destroying,
        Destroyed,
    }
    
    public class ActorContainer
    {
        public string Name { get; set; }
        public Actor Actor { get; set; }
        public List<ActorQueueToken> Tokens { get; set; }
        public ActorStatus Status { get; set; }
        public ActorSchematic Schematic { get; set; }
        public bool Faulted { get; private set; }
        public Exception? Fault { get; set; }
        private readonly object TokenLock = new object();
        
        public ActorContainer(ActorSchematic schematic, string name, Actor actor)
        {
            Name      = name;
            Status    = ActorStatus.Ready;
            Schematic = schematic;
            Actor     = actor;
            Faulted   = false;
            Tokens    = new List<ActorQueueToken>();
        }
        
        public void Enqueue(ActorQueueToken token)
        {
            lock (TokenLock)
            {
                Tokens.Add(token);
            }
        }
        
        public ActorQueueToken? Dequeue()
        {
            lock (TokenLock)
            {
                if (Tokens.Count == 0)
                {
                    return null;
                }
                var token = Tokens.First();
                Tokens.Remove(token);
                return token;
            }
        }
        
        public void SetFault(Exception e)
        {
            Faulted = true;
            Fault   = e;
        }
        
        public void Unfault()
        {
            Faulted = false;
            Fault   = null;
        }
    }
}
