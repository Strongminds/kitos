using Core.Abstractions.Types;
using Core.DomainModel.ItSystem;
using NotImplementedException = System.NotImplementedException;

namespace Core.ApplicationServices;

public class ItS : IItSystemUpdater
{
    public Result<ItSystem, OperationError> UpdateItSystem(ItSystem system)
    {
        throw new NotImplementedException();
    }
}