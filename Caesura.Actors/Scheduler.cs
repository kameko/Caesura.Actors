
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    
    internal class Scheduler
    {
        internal ActorSystem System { get; set; }
        private List<SchedulerAtom> Atoms { get; set; }
        private List<ActorQueueToken> Queue { get; set; }
        
        public Scheduler(ActorSystem system)
        {
            System = system;
            Queue  = new List<ActorQueueToken>();
            Atoms  = new List<SchedulerAtom>(Environment.ProcessorCount);
            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                var atom = new SchedulerAtom(i, this);
                Atoms.Add(atom);
            }
        }
        
        
    }
    
    internal class SchedulerAtom : IDisposable
    {
        public int Id { get; set; }
        public Scheduler Owner { get; set; }
        public bool Running { get; private set; }
        public bool Busy => !(Token is null);
        private ActorQueueToken? Token { get; set; }
        private readonly object TokenLock = new object();
        private Thread AtomThread { get; set; }
        
        public SchedulerAtom(int id, Scheduler owner)
        {
            Id         = id;
            Owner      = owner;
            AtomThread = new Thread(Pump);
            AtomThread.IsBackground = true;
            AtomThread.Start();
            Running    = true;
        }
        
        public bool Enqueue(ActorQueueToken token)
        {
            lock (TokenLock)
            {
                if (Busy)
                {
                    return false;
                }
                
                Token = token;
                return true;
            }
        }
        
        private void Pump()
        {
            while (Running)
            {
                lock (TokenLock)
                {
                    if (!(Token is null))
                    {
                        var container = Owner.System.GetActor(Token.Receiver);
                        if (container is null)
                        {
                            continue;
                        }
                        
                        var actor = container.Actor;
                        
                        var result = actor.ProcessMessage(Token.Sender, Token.Data);
                        // TODO: handle the result
                        
                        Token = null;
                    }
                    else
                    {
                        Thread.Sleep(15);
                    }
                }
            }
        }
        
        public void Dispose()
        {
            Running = false;
        }
    }
}
