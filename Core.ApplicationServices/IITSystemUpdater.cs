using Core.Abstractions.Types;
using Core.DomainModel.ItSystem;

namespace Core.ApplicationServices;

public interface IItSystemUpdater
{
    Result<ItSystem, OperationError> UpdateItSystem(ItSystem system);
}