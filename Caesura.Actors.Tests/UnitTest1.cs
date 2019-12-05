
namespace Caesura.Actors.Tests
{
    using System;
    using Caesura.Actors;
    using Xunit;
    using Xunit.Abstractions;
    
    public class UnitTest1
    {
        private readonly ITestOutputHelper Logger;
        
        public UnitTest1(ITestOutputHelper output)
        {
            Logger = output;
        }
        
        [Fact]
        public void Test1()
        {
            SetupLogger();
            Logger.WriteLine("BEGIN TEST");
            
            var system = ActorSystem.Create("my-system");
            var actor1 = system.NewActor(new ActorSchematic(() => new Actor1()), "actor1");
            actor1.Tell(("LOG", "Hello, world!"), ActorReferences.NoSender);
            actor1.Tell(("LOST LETTER!", "you're just upset you'll never be able to do what I can do with my guitar hands"), ActorReferences.NoSender);
            
            System.Threading.Thread.Sleep(1000);
        }
        
        public void SetupLogger()
        {
            ActorLogger.OnLog += token =>
            {
                try
                {
                    Logger.WriteLine(token.ToString());
                }
                catch (InvalidOperationException)
                {
                    // swallow
                }
            };
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
        }
    }
}
