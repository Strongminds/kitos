using System.Net.Http;
using System.Threading.Tasks;
using Core.ApplicationServices.Model.KitosEvents;

namespace Core.ApplicationServices;

public interface IHttpEventPublisher
{
    Task<HttpResponseMessage> PostEventAsync(KitosEventDTO eventDTO);
}