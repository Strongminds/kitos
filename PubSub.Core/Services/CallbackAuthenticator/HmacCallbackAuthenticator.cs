using Microsoft.Extensions.Configuration;
using PubSub.Core.Services.CallbackAuthentication;
using System.Security.Cryptography;
using System.Text;

namespace PubSub.Core.Services.CallbackAuthenticator
{
    public class HmacCallbackAuthenticator : ICallbackAuthenticator
    {
        private readonly IConfiguration _configuration;

        public HmacCallbackAuthenticator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetAuthentication(string text)
        {
            var key = _configuration["ApiKey"] ?? "";

            using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToBase64String(hash);
        }
    }
}
