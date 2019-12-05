
namespace Caesura.Actors.Tests.Manual
{
    using System;
    using System.Threading.Tasks;
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Beginning test...");
            
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
            var system = ActorSystem.Create("my-system");
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
                    case "SPAWN-ACTOR2":
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
                }
            }
        }
        
        public void SetupLogger()
        {
            ActorLogger.DefaultLoggerEnabled = true;
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
            spawn_actor2 += msg => StrEq(msg.Command, "SPAWN-ACTOR2");
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
                Tell(ping, "DIE");
                Tell(pong, "DIE");
            };
            
            var say_children = Handler<string>.Create(this);
            say_children += msg => StrEq(msg, "CHILDREN");
            say_children += msg =>
            {
                ActorLog.Info($"Children: {Children.Count}");
            };
        }
    }
    
    public class Actor2 : Actor
    {
        protected override void OnCreate()
        {
            Become(Behavior1);
            ActorLog.Info("Hello, world!");
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
                ActorLog.Info("PONG");
                await Task.Delay(Delay);
                Respond("PONG");
            };
            
            var pong = Handler<string>.Create(this);
            pong += msg => StrEq(msg, "PONG");
            pong += async msg =>
            {
                ActorLog.Info("PING");
                await Task.Delay(Delay);
                Respond("PING");
            };
            
            var die = Handler<string>.Create(this);
            die += msg => StrEq(msg, "DIE");
            die += msg =>
            {
                ActorLog.Info("GOODBYE! ;_; ");
                DestroySelf();
            };
        }
    }
}
