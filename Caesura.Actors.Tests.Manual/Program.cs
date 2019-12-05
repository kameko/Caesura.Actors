
namespace Caesura.Actors.Tests.Manual
{
    using System;
    
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
            var actor1 = system.NewActor(new ActorSchematic(() => new Actor1()), "actor1");
            
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
                    case "SPAWN":
                        if (string.IsNullOrEmpty(arguments))
                        {
                            continue;
                        }
                        actor1.Tell((command, arguments), ActorReferences.NoSender);
                        break;
                    case "SWITCH":
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
    
    public class Actor1 : Actor
    {
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
            
            var spawn = Handler<(string Command, string Name)>.Create(this);
            spawn += msg => StrEq(msg.Command, "SPAWN");
            spawn += msg =>
            {
                ActorLog.Info("Spawning child...");
                // NewChild(new ActorSchematic(() => new Actor2()), $"babby{Children.Count}");
                NewChild(new ActorSchematic(() => new Actor2()), msg.Name);
            };
            
            var switch_child = Handler<string>.Create(this);
            switch_child += msg => StrEq(msg, "SWITCH");
            switch_child += msg =>
            {
                ActorLog.Info($"Telling children to switch behaviors. Children: {Children.Count}");
                TellChildren("SWITCH");
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
}
