
namespace Caesura.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    
    public interface IStateSerializeOptions
    {
        
    }
    
    public interface IStateDeserializeOptions
    {
        
    }
    
    public interface IState
    {
        ActorPath Origin { get; set; }
    }
    
    public interface IState<TData, TSerialized> : IState
    {
        TSerialized Serialized();
        TSerialized Serialized(IStateSerializeOptions options);
        TData Deserialize();
        TData Deserialize(IStateDeserializeOptions options);
    }
}
