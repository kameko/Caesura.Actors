
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public interface IStateSerializeHandler
    {
        
    }
    
    public interface IStateDeserializeHandler
    {
        
    }
    
    public interface IState
    {
        ActorPath Origin { get; set; }
    }
    
    public interface IState<TData, TSerialized> : IState
    {
        TSerialized Serialize();
        TSerialized Serialize(IStateSerializeHandler options);
        TData Deserialize();
        TData Deserialize(IStateDeserializeHandler options);
    }
}
