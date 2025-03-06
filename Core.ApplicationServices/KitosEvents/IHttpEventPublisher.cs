using System.Net.Http;
using Core.ApplicationServices.Model.KitosEvents;
using System.Threading.Tasks;

namespace Core.ApplicationServices.KitosEvents;

public interface IHttpEventPublisher
{
    Task<HttpResponseMessage> PostEventAsync(KitosEventDTO eventDTO);
}