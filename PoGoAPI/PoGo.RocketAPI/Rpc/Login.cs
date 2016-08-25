using System;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Login;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using System.Net;
using POGOProtos.Networking.Envelopes;

namespace PokemonGo.RocketAPI.Rpc
{
    public delegate void GoogleDeviceCodeDelegate(string code, string uri);
    public class Login : BaseRpc
    {
        //public event GoogleDeviceCodeDelegate GoogleDeviceCodeEvent;
        private ILoginType login;

        public Login(Client client) : base(client)
        {
            login = SetLoginType(client.Settings);
        }

        private static ILoginType SetLoginType(ISettings settings)
        {
            WebProxy prox = null;
            if (settings.UseProxy)
            {
                NetworkCredential proxyCreds = null;
                if (settings.ProxyLogin != "")
                    proxyCreds = new NetworkCredential(settings.ProxyLogin, settings.ProxyPass);
                prox = new WebProxy(settings.ProxyUri)
                {
                    UseDefaultCredentials = false,
                    Credentials = proxyCreds,
                };
            }

            switch (settings.AuthType)
            {
                case AuthType.Google:
                    return new GoogleLogin(settings.GoogleUsername, settings.GooglePassword, prox);
                case AuthType.Ptc:
                    return new PtcLogin(settings.PtcUsername, settings.PtcPassword, prox);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.AuthType), "Unknown AuthType");
            }
        }

        public async Task DoLogin()
        {
            _client.AuthToken = await login.GetAccessToken().ConfigureAwait(false);
            await SetServer().ConfigureAwait(false);
        }

        private async Task SetServer()
        {
            #region Standard intial request messages in right Order

            var getPlayerMessage = new GetPlayerMessage();
            var getHatchedEggsMessage = new GetHatchedEggsMessage();
            var getInventoryMessage = new GetInventoryMessage
            {
                LastTimestampMs = DateTime.UtcNow.ToUnixTime()
            };
            var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = "54b359c97e46900f87211ef6e6dd0b7f2a3ea1f5" //"05daf51635c82611d1aac95c0b051d3ec088a930"
            };

            #endregion

            var serverRequest = RequestBuilder.GetInitialRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.GetPlayer,
                    RequestMessage = getPlayerMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.GetHatchedEggs,
                    RequestMessage = getHatchedEggsMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = getInventoryMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.CheckAwardedBadges,
                    RequestMessage = checkAwardedBadgesMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });


            var serverResponse = await PostProto<Request>(Resources.RpcUrl, serverRequest);

            if (serverResponse.AuthTicket == null)
            {
                _client.AuthToken = null;
                throw new AccessTokenExpiredException();
            }

            _client.AuthTicket = serverResponse.AuthTicket;
            _client.ApiUrl = serverResponse.ApiUrl;
        }

        public void UpdateHash()
        {
            RequestBuilder.GenerateNewHash();
        }

        public async Task UpdateApiTicket()
        {
            var getPlayerMessage = new GetPlayerMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = "54b359c97e46900f87211ef6e6dd0b7f2a3ea1f5" //"05daf51635c82611d1aac95c0b051d3ec088a930"
            };

            var serverRequest = RequestBuilder.GetInitialRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.GetPlayer,
                    RequestMessage = getPlayerMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });

            var serverResponse = await PostProto<Request>(Resources.RpcUrl, serverRequest);

            if (serverResponse?.AuthTicket != null)
            {
                _client.AuthTicket.MergeFrom(serverResponse.AuthTicket);
            }

        }

    }
}
