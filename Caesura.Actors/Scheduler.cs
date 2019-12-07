
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    
    internal class Scheduler : IScheduler
    {
        internal ActorSystem System { get; set; }
        private List<ActorContainer> Queue { get; set; }
        private List<SessionToken> PersistedActors { get; set; }
        private bool Sleeping { get; set; }
        private bool IsRunning { get; set; }
        private Thread SchedulerThread { get; set; }
        private CancellationTokenSource CancelToken { get; set; }
        
        private readonly object QueueLock = new object();
        private readonly object PersistedLock = new object();
        
        public Scheduler()
        {
            System          = null!;
            Queue           = new List<ActorContainer>();
            PersistedActors = new List<SessionToken>();
            SchedulerThread = new Thread(SchedulerHandler);
            SchedulerThread.IsBackground = true;
            CancelToken     = new CancellationTokenSource();
        }
        
        public void AssignSystem(ActorSystem system)
        {
            System               = system;
            SchedulerThread.Name = CreateName();
        }
        
        public void Start()
        {
            if (!IsRunning)
            {
                try
                {
                    System.Log.Debug($"Starting {SchedulerThread.Name}");
                    CancelToken = new CancellationTokenSource();
                    IsRunning = true;
                    SchedulerThread.Start();
                }
                catch (ThreadStateException e)
                {
                    // We don't care
                    System.Log.Verbose(e, $"{SchedulerThread.Name}");
                }
            }
            else
            {
                System.Log.Debug($"Tried starting {SchedulerThread.Name} but it is already running");
            }
        }
        
        public void Stop()
        {
            System.Log.Debug($"Stopping {SchedulerThread.Name}");
            IsRunning = false;
            CancelToken.Cancel();
        }
        
        public void InformActorDestruction(ActorContainer container)
        {
            lock (QueueLock)
            {
                if (Queue.Contains(container))
                {
                    BeginSessionPersistence(System.Lost);
                    Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = System.Lost.Path.Path;
                        foreach (var token in container.Dump())
                        {
                            if (CancelToken.IsCancellationRequested)
                            {
                                break;
                            }
                            
                            var lost_letter = new LostLetter(token.Sender, token.Receiver, token.Data);
                            System.Lost.ProcessMessage(token.Sender, token.Data, CancelToken.Token);
                        }
                    },
                    CancelToken.Token)
                    .ContinueWith(_ =>
                    {
                        EndSessionPersistence(System.Lost);
                    });
                    
                    Queue.Remove(container);
                }
            }
        }
        
        public void Enqueue(ActorContainer container, ActorQueueToken token)
        {
            if (Sleeping)
            {
                Spinup();
            }
            
            container.Enqueue(token);
            
            lock (QueueLock)
            {
                Queue.Add(container);
            }
        }
        
        public void BeginSessionPersistence(Actor actor)
        {
            lock (PersistedLock)
            {
                var token = PersistedActors.Find(x => x.Path == actor.Path);
                if (token is null)
                {
                    PersistedActors.Add(new SessionToken(actor.Path));
                }
                else
                {
                    token.Count++;
                }
            }
        }
        
        public void EndSessionPersistence(Actor actor)
        {
            lock (PersistedLock)
            {
                var token = PersistedActors.Find(x => x.Path == actor.Path);
                if (!(token is null))
                {
                    token.Count--;
                    if (token.Count <= 0)
                    {
                        PersistedActors.Remove(token);
                    }
                }
            }
        }
        
        private void SchedulerHandler()
        {
            var po = new ParallelOptions();
            po.CancellationToken = CancelToken.Token;
            if (System.Config.ParallelSchedulerMaxThreads < 1)
            {
                po.MaxDegreeOfParallelism = Environment.ProcessorCount;
            }
            else
            {
                po.MaxDegreeOfParallelism = System.Config.ParallelSchedulerMaxThreads;
            }
            
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
                        if (System.Config.SpinDownFreeScheduler)
                        {
                            // TODO: check if it's been like, a second or two
                            // if so, Spindown
                        }
                        
                        continue;
                    }
                    else
                    {
                        lock (PersistedLock)
                        {
                            if (System.Config.ParallelScheduler)
                            {
                                try
                                {
                                    Parallel.ForEach(Queue, po, container =>
                                    {
                                        Process(container);
                                    });
                                }
                                catch (OperationCanceledException e)
                                {
                                    System.Log.Info(e, $"Parallel operation canceled in {SchedulerThread.Name}");
                                }
                                catch (AggregateException e)
                                {
                                    System.Log.Info(e, $"Error in parallel operation in {SchedulerThread.Name}");
                                }
                            }
                            else
                            {
                                var container = Queue.First();
                                Process(container);
                            }
                        }
                    }
                }
            }
        }
        
        private void Process(ActorContainer container)
        {
            if (PersistedActors.Exists(x => x.Path == container.Actor.Path))
            {
                ReEnqueue();
                return;
            }
            else
            {
                Queue.Remove(container);
                
                var token = container.Dequeue();
                if (token is null)
                {
                    System.Log.Debug($"Scheduled actor {container.Actor.Path} but the container's queue was empty");
                    return;
                }
                
                if (container is null)
                {
                    BeginSessionPersistence(System.Lost);
                    Task.Run(() =>
                    {
                        Thread.CurrentThread.Name = token.Receiver.Path;
                        
                        var lost_letter = new LostLetter(token.Sender, token.Receiver, token.Data);
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
                    // put it back in the queue for now
                    Queue.Add(container);
                    return;
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
            var name = $"scheduler.{ActorSystem.SystemCount}.{System.Location}";
            return name;
        }
        
        private class SessionToken
        {
            public ActorPath Path { get; set; }
            public int Count { get; set; }
            
            public SessionToken(ActorPath path)
            {
                Path  = path;
                Count = 1;
            }
        }
    }
}
