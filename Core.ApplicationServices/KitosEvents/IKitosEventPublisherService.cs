using Core.Abstractions.Types;
using Core.ApplicationServices.Model.KitosEvents;
using System.Threading.Tasks;

namespace Core.ApplicationServices.KitosEvents;

public interface IKitosEventPublisherService
{
    Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething);
}