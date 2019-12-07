
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    internal class LostLetters : Actor
    {
        internal ActorSystem System { get; set; }
        
        internal LostLetters(ActorSystem system)
        {
            System = system;
        }
        
        protected override void OnCreate()
        {
            Become(Behavior);
        }
        
        private void Behavior()
        {
            var on_lost = Handle<LostLetter>();
            on_lost += letter =>
            {
                if (!System.Config.LogLostLetters)
                {
                    return;
                }
                
                ActorLog.Info(
                    $"Message from \"{letter.Sender.Path.Path}\" containing " +
                    $"{letter.Data.GetType().Name} did not reach target " +
                    $"at \"{letter.Receiver.Path}\". " +
                    "Data: {0}", new object[] { letter.Data }
                );
            };
            
            var on_any = AnyHandler();
            on_any += msg =>
            {
                ActorLog.Warning(
                    $"{nameof(LostLetters)} got a message that was not of {nameof(LostLetter)}!"
                );
            };
        }
    }
    
    internal class LostLetter
    {
        public IActorReference Sender { get; set; }
        public ActorPath Receiver { get; set; }
        public object Data { get; set; }
        
        public LostLetter(IActorReference sender, ActorPath receiver, object data)
        {
            Sender   = sender;
            Receiver = receiver;
            Data     = data;
        }
    }
}
