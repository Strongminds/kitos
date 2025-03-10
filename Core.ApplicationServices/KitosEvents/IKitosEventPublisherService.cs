using Core.ApplicationServices.Model.KitosEvents;
using System.Threading.Tasks;

namespace Core.ApplicationServices.KitosEvents;

public interface IKitosEventPublisherService
{
    Task PublishEvent(KitosEvent kitosEvent);
}