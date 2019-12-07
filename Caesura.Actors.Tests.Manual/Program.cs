
namespace Caesura.Actors.Tests.Manual
{
    using System;
    using System.Threading.Tasks;
    
    class Program
    {
        static void Main(string[] args)
        {
            // Console.WriteLine("Beginning test...");
            
            var test1 = new Test1();
            test1.Start();
        }
    }
    
    public class Test1
    {
        public void Start()
        {
            Console.WriteLine("Starting system...");
            
            SetupLogger();
            
            var config = new ActorsConfiguration()
            {
                VerboseLogAllMessages = true,
                // ParallelScheduler = true,
            };
            
            var system = ActorSystem.Create("my-system", config);
            var actor1 = system.NewActor(new ActorSchematic(() => new Supervisor()), "supervisor");
            
            var running = true;
            while (running)
            {
                var input = Console.ReadLine();
                
                if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }
                
                var inputs = input.Split(' ');
                var command = inputs[0];
                var arguments = string.Empty;
                if (inputs.Length > 1)
                {
                    arguments = input.Substring(command.Length + 1);
                }
                
                switch (command.ToUpper())
                {
                    case "EXIT":
                    case "END":
                    case "QUIT":
                    case "Q":
                    case ".":
                        running = false;
                        break;
                    case "LOG":
                        if (string.IsNullOrEmpty(arguments))
                        {
                            continue;
                        }
                        actor1.Tell((command, arguments), ActorReferences.NoSender);
                        break;
                    case "SPAWN-ACTOR":
                        if (string.IsNullOrEmpty(arguments))
                        {
                            continue;
                        }
                        actor1.Tell((command, arguments), ActorReferences.NoSender);
                        break;
                    case "PINGPONG":
                        actor1.Tell((command, arguments), ActorReferences.NoSender);
                        break;
                    case "STOP-PINGPONG":
                        actor1.Tell(command, ActorReferences.NoSender);
                        break;
                    case "CHILDREN":
                        actor1.Tell(command, ActorReferences.NoSender);
                        break;
                    case "FAULT":
                        actor1.Tell(command, ActorReferences.NoSender);
                        break;
                    default:
                        Console.WriteLine("?");
                        break;
                }
            }
        }
        
        public void SetupLogger()
        {
            ActorLogger.DefaultLoggerEnabled = true;
            ActorLogger.VERBOSE = true;
        }
    }
    
    public class Supervisor : Actor
    {
        private (IActorReference, IActorReference) PingAndPong { get; set; }
        
        protected override void OnCreate()
        {
            Become(Behavior1);
        }
        
        private void Behavior1()
        {
            var respond = Handler<(string Command, string Message)>.Create(this);
            respond += msg => StrEq(msg.Command, "LOG");
            respond += msg =>
            {
                ActorLog.Info(msg.Message);
            };
            
            var spawn_actor2 = Handler<(string Command, string Name)>.Create(this);
            spawn_actor2 += msg => StrEq(msg.Command, "SPAWN-ACTOR");
            spawn_actor2 += msg =>
            {
                ActorLog.Info("Spawning child...");
                // NewChild(new ActorSchematic(() => new Actor2()), $"babby{Children.Count}");
                NewChild(new ActorSchematic(() => new Actor2()), msg.Name);
            };
            
            var spawn_pingpong = Handler<(string Command, string Delay)>.Create(this);
            spawn_pingpong += msg => StrEq(msg.Command, "PINGPONG");
            spawn_pingpong += msg =>
            {
                var delay = 0;
                var success = int.TryParse(msg.Delay, out delay);
                if (!success)
                {
                    delay = 500;
                }
                ActorLog.Info("Spawning children...");
                var ping = NewChild(new ActorSchematic(() => new Actor3(delay)), "ping");
                var pong = NewChild(new ActorSchematic(() => new Actor3(delay)), "pong");
                PingAndPong = (ping, pong);
                ping.Tell("PING", pong);
            };
            
            var end_pingpong = Handler<string>.Create(this);
            end_pingpong += msg => StrEq(msg, "STOP-PINGPONG");
            end_pingpong += msg =>
            {
                var ping = PingAndPong.Item1;
                var pong = PingAndPong.Item2;
                Tell(ping, Halt.Instance);
                Tell(pong, Halt.Instance);
            };
            
            var say_children = Handler<string>.Create(this);
            say_children += msg => StrEq(msg, "CHILDREN");
            say_children += msg =>
            {
                ActorLog.Info($"Children: {Children.Count}");
            };
            
            var fault_child = Handler<string>.Create(this);
            fault_child += msg => StrEq(msg, "FAULT");
            fault_child += msg =>
            {
                TellChildren("FAULT");
            };
            
            var on_fault = Handler<Fault>.Create(this);
            on_fault += fault =>
            {
                if (fault.Exception.Message == "RESTART ME")
                {
                    ActorLog.Info($"Restarting child {fault.FaultedActor}");
                    fault.Restart();
                }
                else
                {
                    ActorLog.Info($"Destroying child {fault.FaultedActor}");
                    fault.Destroy();
                }
            };
        }
    }
    
    public class Actor2 : Actor
    {
        public Actor2()
        {
            // throw new Exception("oops!");
        }
        
        protected override void OnCreate()
        {
            Become(Behavior1);
            ActorLog.Info("Hello, world!");
            // throw new Exception("oops!");
        }
        
        protected override void PreReload()
        {
            ActorLog.Info("Preloading");
        }
        
        protected override void PostReload()
        {
            ActorLog.Info("Post Reloading");
        }
        
        private void Behavior1()
        {
            var respond = Handler<string>.Create(this);
            respond += msg => StrEq(msg, "SWITCH");
            respond += msg =>
            {
                ActorLog.Info("Switching to Behavior2");
                Become(Behavior2);
            };
            
            CommonBehavior();
        }
        
        private void Behavior2()
        {
            var respond = Handler<string>.Create(this);
            respond += msg => StrEq(msg, "SWITCH");
            respond += msg =>
            {
                ActorLog.Info("Switching to Behavior1");
                Become(Behavior1);
            };
            
            CommonBehavior();
        }
        
        private void CommonBehavior()
        {
            var fault_self = Handler<string>.Create(this);
            fault_self += msg => StrEq(msg, "FAULT");
            fault_self += (Action<string>)(msg =>
            {
                throw new Exception("RESTART ME");
            });
        }
    }
    
    public class Actor3 : Actor
    {
        private int Delay { get; set; }
        
        public Actor3(int delay)
        {
            Delay = delay;
        }
        
        protected override void OnCreate()
        {
            Become(Behavior1);
            ActorLog.Info("Hello, world!");
        }
        
        private void Behavior1()
        {
            var ping = Handler<string>.Create(this);
            ping += msg => StrEq(msg, "PING");
            ping += async msg =>
            {
                await Task.Delay(Delay);
                Respond("PONG");
            };
            
            var pong = Handler<string>.Create(this);
            pong += msg => StrEq(msg, "PONG");
            pong += async msg =>
            {
                await Task.Delay(Delay);
                Respond("PING");
            };
        }
    }
}
