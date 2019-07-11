using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Helpers
{
    internal class RetryHandler : DelegatingHandler
    {
        private const int MaxRetries = 25;

        public RetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i <= MaxRetries; i++)
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
                    if (response.StatusCode == HttpStatusCode.BadGateway ||
                        response.StatusCode == HttpStatusCode.InternalServerError)
                        throw new Exception($"{response.StatusCode}"); //todo: proper implementation

                    return response;
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[#{i} of {MaxRetries}] retry request {request.RequestUri} - Error: {ex.Message}");
                    if (i < MaxRetries)
                    {
                        await Task.Delay(i * 500 + 1000, cancellationToken);
                        continue;
                    }
                    throw;
                }
            }
            return null;
        }
    }
}