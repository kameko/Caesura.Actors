
namespace Caesura.Actors.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public class MessageStash
    {
        private ActorSystem System { get; set; }
        private Actor Owner { get; set; }
        private List<StashAtom> Atoms { get; set; }
        private readonly object AtomLock = new object();
        
        public MessageStash(ActorSystem system, Actor owner)
        {
            System = system;
            Owner  = owner;
            Atoms  = new List<StashAtom>();
        }
        
        public void Stash()
        {
            if (Owner.Handlers.CurrentMessage is null)
            {
                return;
            }
            
            var atom = new StashAtom(Owner.InternalSender, Owner.Handlers.CurrentMessage);
            
            lock (AtomLock)
            {
                Atoms.Add(atom);
            }
        }
        
        public void Unstash()
        {
            if (Atoms.Count == 0)
            {
                return;
            }
            
            lock (AtomLock)
            {
                var atom = Atoms.First();
                Atoms.Remove(atom);
                
                System.EnqueueForMessageProcessing(Owner.Path, atom.Message, atom.Sender);
            }
        }
        
        public void UnstashAll()
        {
            lock (AtomLock)
            {
                foreach (var atom in Atoms)
                {
                    System.EnqueueForMessageProcessing(Owner.Path, atom.Message, atom.Sender);
                }
                Atoms.Clear();
            }
        }
    }
    
    internal class StashAtom
    {
        public IActorReference Sender { get; set; }
        public object Message { get; set; }
        
        public StashAtom(IActorReference sender, object message)
        {
            Sender  = sender;
            Message = message;
        }
    }
}
