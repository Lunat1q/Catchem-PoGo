using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Login;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using System.Net;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public delegate void GoogleDeviceCodeDelegate(string code, string uri);
    public class Login : BaseRpc
    {
        //public event GoogleDeviceCodeDelegate GoogleDeviceCodeEvent;
        private readonly ILoginType _login;

        public Login(Client client) : base(client)
        {
            _login = SetLoginType(client.Settings);
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
            _client.AuthToken = await _login.GetAccessToken().ConfigureAwait(false);
            await SetServer().ConfigureAwait(false);
        }

        public async Task DoLogin(bool noRetry)
        {
            _client.AuthToken = await _login.GetAccessToken().ConfigureAwait(false);
            await SetServer(noRetry).ConfigureAwait(false);
        }

        private async Task SetServer(bool noRetry = false)
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
                Hash = _downloadHash //"05daf51635c82611d1aac95c0b051d3ec088a930"
            };

            var downloadRemoteConfigMessage = new DownloadRemoteConfigVersionMessage
            {
                Platform = Platform.Ios,
                AppVersion = 4500
            };

            #endregion

            try
            {

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
                    },
                    new Request
                    {
                        RequestType = RequestType.DownloadSettings,
                        RequestMessage = downloadSettingsMessage.ToByteString()
                    },
                    new Request
                    {
                        RequestType = RequestType.DownloadRemoteConfigVersion,
                        RequestMessage = downloadRemoteConfigMessage.ToByteString()
                    });
                var serverResponse = await PostProto<Request>(Resources.RpcUrl, serverRequest);

                if (serverResponse.AuthTicket == null)
                {
                    _client.AuthToken = null;
                    throw new AccessTokenExpiredException(
                        "Check your internet connection and try to restart the profile");
                }
                _client.AuthTicket = serverResponse.AuthTicket;
                _client.ApiUrl = serverResponse.ApiUrl;
                

            /*    var dlVerReq = RequestBuilder.GetRequestEnvelope(new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });

                var dlVerResp = await PostProtoPayload<Request, DownloadSettingsResponse>(RequestType.DownloadSettings, dlVerReq);
            */
            }
            catch (Exception)
            {
                if (!noRetry)
                {
                    await Task.Delay(15000);
                    await DoLogin();
                }
                else
                {
                    throw new Exception("Check your internet connection and try to restart the profile");
                }
            }
        }

        public void UpdateHash()
        {
            RequestBuilder.GenerateNewHash();
        }

        public async Task UpdateApiTicket(int attempt = 0)
        {
            var getPlayerMessage = new GetPlayerMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = _downloadHash
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
            else if (attempt < 5)
            {
                Debug.WriteLine("Requesting new OAuth token, old one is outdated");
                try
                {
                    _client.AuthToken = await _login.GetAccessToken().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await Task.Delay(15000);
                    await _client.Login.DoLogin();
                    return;
                }

                if (_client.AuthToken == null)
                {
                    await Task.Delay(15000);
                    await _client.Login.DoLogin();
                }
                else
                {
                    await UpdateApiTicket(++attempt);
                }
            }
        }

    }
}
