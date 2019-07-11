using DankMemes.GPSOAuthSharp;
using PokemonGo.RocketAPI.Exceptions;
using System.Net;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Login
{
    public class GoogleLogin : ILoginType
    {
        private readonly string password;
        private readonly string email;
        private readonly IWebProxy proxy;

        public GoogleLogin(string email, string password, IWebProxy _proxy)
        {
            this.email = email;
            this.password = password;
            proxy = _proxy;
        }

        public async Task<string> GetAccessToken()
        {
            var client = new GPSOAuthClient(email, password, proxy);
            var response = client.PerformMasterLogin();

            if (response.ContainsKey("Error"))
                throw new GoogleException(response["Error"]);

            //Todo: captcha/2fa implementation

            if (!response.ContainsKey("Auth"))
                throw new GoogleOfflineException();

            var oauthResponse = client.PerformOAuth(response["Token"],
                "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com",
                "com.nianticlabs.pokemongo",
                "321187995bc7cdc2b5fc91b11a96e2baa8602c62");

            if (!oauthResponse.ContainsKey("Auth"))
                throw new GoogleOfflineException();

            await Task.Delay(1);

            return oauthResponse["Auth"];
        }
    }
}