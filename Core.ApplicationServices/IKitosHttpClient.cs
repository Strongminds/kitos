using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Core.ApplicationServices;

public interface IKitosHttpClient
{
    Task<HttpResponseMessage> PostAsync(object content, Uri uri);
}