
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
        
        private readonly object QueueLock = new object();
        private readonly object PersistedLock = new object();
        
        public Scheduler(ActorSystem system)
        {
            System          = system;
            Queue           = new List<ActorQueueToken>();
            PersistedActors = new List<Actor>();
            SchedulerThread = new Thread(SchedulerHandler);
            SchedulerThread.IsBackground = true;
            SchedulerThread.Name         = CreateName();
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
            
            lock (QueueLock)
            {
                Queue.Add(token);
            }
        }
        
        public void BeginSessionPersistence(Actor actor)
        {
            lock (PersistedLock)
            {
                if (PersistedActors.Contains(actor))
                {
                    // throw new InvalidOperationException($"Actor {actor.Path} is already having it's session persisted");
                    
                    // do nothing
                    return;
                }
                
                PersistedActors.Add(actor);
            }
        }
        
        public void EndSessionPersistence(Actor actor)
        {
            lock (PersistedLock)
            {
                if (!PersistedActors.Contains(actor))
                {
                    // throw new InvalidOperationException($"Actor {actor.Path} has not been persisted");
                    
                    // do nothing
                    return;
                }
                
                PersistedActors.Remove(actor);
            }
        }
        
        private void SchedulerHandler()
        {
            while (IsRunning)
            {
                if (CancelToken.IsCancellationRequested)
                {
                    break;
                }
                
                if (Sleeping)
                {
                    Thread.Sleep(15);
                    
                    if (CancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    continue;
                }
                
                lock (QueueLock)
                {
                    if (Queue.Count == 0)
                    {
                        // TODO: check if it's been like, a second or two
                        // if so, Spindown
                        
                        continue;
                    }
                    else
                    {
                        lock (PersistedLock)
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
                                
                                var container = System.GetActor(token.Receiver);
                                
                                if (container is null)
                                {
                                    BeginSessionPersistence(System.Lost);
                                    Task.Run(() =>
                                    {
                                        Thread.CurrentThread.Name = token.Receiver.Path;
                                        
                                        var lost_receiver = System.GetReference(token.Receiver);
                                        var lost_letter = new LostLetter(token.Sender, lost_receiver, token.Data);
                                        System.Lost.ProcessMessage(token.Sender, token.Data, CancelToken.Token);
                                    },
                                    CancelToken.Token)
                                    .ContinueWith(_ =>
                                    {
                                        EndSessionPersistence(System.Lost);
                                    });
                                }
                                else if (container.Faulted)
                                {
                                    ReEnqueue();
                                    continue;
                                }
                                else
                                {
                                    var actor = container.Actor;
                                    
                                    BeginSessionPersistence(container.Actor);
                                    Task.Run(() =>
                                    {
                                        Thread.CurrentThread.Name = token.Receiver.Path;
                                        actor.ProcessMessage(token.Sender, token.Data, CancelToken.Token);
                                    },
                                    CancelToken.Token)
                                    .ContinueWith(_ =>
                                    {
                                        EndSessionPersistence(container.Actor);
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void ReEnqueue()
        {
            lock (QueueLock)
            {
                var token = Queue.First();
                Queue.Remove(token);
                Queue.Add(token);
            }
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
                return $"{name} {ActorSystem.SystemCount + 1}";
            }
        }
    }
}
