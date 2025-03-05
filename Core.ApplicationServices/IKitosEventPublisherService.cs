using Core.Abstractions.Types;
using System.Threading.Tasks;

namespace Core.ApplicationServices
{
    public interface IKitosEventPublisherService
    {
        Task<Maybe<OperationError>> PublishEvent(KitosEvent eventSomething);
    }
}