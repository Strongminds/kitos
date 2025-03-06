using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure.Services.Http;

public class KitosHttpClient : IKitosHttpClient
{
    public Task<HttpResponseMessage> PostAsync(object content, Uri uri, string token)
    {
        throw new NotImplementedException();
    }
}