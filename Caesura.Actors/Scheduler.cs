
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
        private List<ActorQueueToken> Queue { get; set; }
        private List<Actor> PersistedActors { get; set; }
        private bool Sleeping { get; set; }
        private bool IsRunning { get; set; }
        private Thread SchedulerThread { get; set; }
        private CancellationTokenSource CancelToken { get; set; }
        
        public Scheduler(ActorSystem system)
        {
            System          = system;
            Queue           = new List<ActorQueueToken>();
            PersistedActors = new List<Actor>();
            SchedulerThread = new Thread(SchedulerHandler);
            SchedulerThread.IsBackground = true;
            SchedulerThread.Name = CreateName();
            CancelToken     = new CancellationTokenSource();
        }
        
        public void Start()
        {
            if (!IsRunning)
            {
                try
                {
                    CancelToken = new CancellationTokenSource();
                    IsRunning = true;
                    SchedulerThread.Start();
                }
                catch (ThreadStateException)
                {
                    // We don't care
                }
            }
        }
        
        public void Stop()
        {
            IsRunning = false;
            CancelToken.Cancel();
        }
        
        public void Enqueue(ActorQueueToken token)
        {
            if (Sleeping)
            {
                Spinup();
            }
            Queue.Add(token);
        }
        
        public void BeginSessionPersistence(Actor actor)
        {
            if (PersistedActors.Contains(actor))
            {
                throw new InvalidOperationException($"Actor {actor.Path} is already having it's session persisted");
            }
            
            PersistedActors.Add(actor);
        }
        
        public void EndSessionPersistence(Actor actor)
        {
            if (!PersistedActors.Contains(actor))
            {
                throw new InvalidOperationException($"Actor {actor.Path} has not been persisted");
            }
            
            PersistedActors.Remove(actor);
        }
        
        private void SchedulerHandler()
        {
            while (IsRunning)
            {
                if (Queue.Count == 0)
                {
                    // TODO: check if it's been like, a second or two
                    // if so, Spindown
                    
                    continue;
                }
                
                if (Sleeping)
                {
                    Thread.Sleep(15);
                }
                else
                {
                    var token = Queue.First();
                    if (PersistedActors.Exists(x => x.Path == token.Receiver))
                    {
                        ReEnqueue();
                        continue;
                    }
                    else
                    {
                        Queue.Remove(token);
                        var actor = System.GetActor(token.Receiver);
                        if (!(actor is null))
                        {
                            Task.Run(() =>
                            {
                                Thread.CurrentThread.Name = token.Receiver.Path;
                                actor.Actor.ProcessMessage(token.Sender, token.Data, CancelToken.Token);
                            },
                            CancelToken.Token);
                        }
                        else
                        {
                            System.Unhandled(token.Sender, token.Receiver, token.Data);
                        }
                    }
                }
            }
        }
        
        private void ReEnqueue()
        {
            var token = Queue.First();
            Queue.Remove(token);
            Queue.Add(token);
        }
        
        private void Spinup()
        {
            Sleeping = false;
        }
        
        private void Spindown()
        {
            Sleeping = true;
        }
        
        private string CreateName()
        {
            var name = "Actor Scheduler";
            if (ActorSystem.SystemCount == 0)
            {
                return name;
            }
            else
            {
                return $"{name} {ActorSystem.SystemCount}";
            }
        }
    }
}
