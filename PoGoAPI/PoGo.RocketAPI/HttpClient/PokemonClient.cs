using System.Net;
using System.Net.Http;
using PokemonGo.RocketAPI.Helpers;

namespace PokemonGo.RocketAPI.HttpClient
{
    public class PokemonHttpClient : System.Net.Http.HttpClient
    {
        private static HttpClientHandler Handler(IWebProxy proxy)
        {
            return new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false,
                Proxy = proxy
            };
        }

        public PokemonHttpClient(IWebProxy proxy) : base(new RetryHandler(Handler(proxy)))
        {
            
            DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Niantic App");
            DefaultRequestHeaders.ExpectContinue = false;
            DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        }
    }
}
