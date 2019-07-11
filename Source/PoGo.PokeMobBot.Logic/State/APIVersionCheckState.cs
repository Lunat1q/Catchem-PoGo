using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.GUI;

namespace PoGo.PokeMobBot.Logic.State
{
    public class ApiVersionCheckState
    {
        public const string ApiVersionUri = "https://raw.githubusercontent.com/Lunat1q/Catchem-PoGo/master/API_version.json";
        public const string NianticApiVersionUri = "https://pgorelease.nianticlabs.com/plfe/version";

        public static async Task<bool> Execute(ISession session, CancellationToken cancellationToken)
        {
           cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var nianticVersion = await DownloadNianticApiVersion(session);
                var version = await DownloadApiVersion(session);
                if (version.Catchem_Version == "" || version.Minimum_Version == "" || nianticVersion == "")
                {
                    session.EventDispatcher.Send(new UpdateEvent
                    {
                        //"Unable to get API version Information"
                        Message = session.Translation.GetTranslation(TranslationString.CantGetAPIVersion)
                    });
                    return false;
                }
                var splitCatchemVersion = GetVersion(version.Catchem_Version);
                var splitMinimumVersion = GetVersion(version.Minimum_Version);
                var splitNianticVersion = GetVersion(nianticVersion);

                if (splitMinimumVersion > splitCatchemVersion || splitNianticVersion > splitCatchemVersion)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        //"Catchem API version is less than the minimum stop botting to avoid ban!"
                        Message = session.Translation.GetTranslation(TranslationString.OldAPIErr)
                    });
                    return false;
                }
                session.EventDispatcher.Send(new UpdateEvent
                {
                    // $"Catchem API Version {version.Catchem_Version}, Minimum API Version {version.Minimum_Version}"
                    Message =
                        session.Translation.GetTranslation(TranslationString.APIVersionOK, version.Catchem_Version,
                            nianticVersion)
                });
                return true;
            }
            catch (Exception)
            {
                session.EventDispatcher.Send(new UpdateEvent
                {
                    Message = "Unable to get API version Information"
                });
                return false;
            }
        }

        private static async Task<APIVersion> DownloadApiVersion(ISession session)
        {
            using (var wC = new WebClient { Proxy = session.Proxy })
            {
                var jsonString =  await wC.DownloadStringTaskAsync(ApiVersionUri);
                var version = JsonConvert.DeserializeObject<APIVersion>(jsonString);
                return version;
            }
         }

        private static async Task<string> DownloadNianticApiVersion(ISession session)
        {
            using (var wC = new WebClient { Proxy = session.Proxy })
            {
                var version = await wC.DownloadStringTaskAsync(NianticApiVersionUri);
                version = version.Remove(0, 2);
                return version;
            }
        }
        private static Version GetVersion(string ver)
        {
            var splitVersion = ver.Split('.');
            var v = new string[3];
            for (var i = 0; i < 3; i++)
                v[i] = "0";
            
            for (var i = 0; i < splitVersion.Length; i++)
                v[i] = splitVersion[i];
            
            return new Version($"{v[0]}.{v[1]}.{v[2]}");
        }

    }

    public class APIVersion
    {
        public string Catchem_Version { get; set; }
        public string Minimum_Version { get; set; }
    }
}
