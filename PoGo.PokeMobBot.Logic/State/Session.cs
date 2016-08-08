#region using directives

using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using System.Net;
using GeoCoordinatePortable;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public interface ISession
    {
        ISettings Settings { get; }
        Inventory Inventory { get; }
        Client Client { get; }
        GetPlayerResponse Profile { get; set; }
        Navigation Navigation { get; }
        ILogicSettings LogicSettings { get; }
        ITranslation Translation { get; }
        IEventDispatcher EventDispatcher { get; }
        IWebProxy Proxy { get; }
        GeoCoordinate ForceMoveTo { get; set; }

        void StartForceMove(double lat, double lng);
    }


    public class Session : ISession
    {
        public Session(ISettings settings, ILogicSettings logicSettings)
        {
            Settings = settings;
            LogicSettings = logicSettings;
            EventDispatcher = new EventDispatcher();
            Translation = Common.Translation.Load(logicSettings);
            Reset(settings, LogicSettings);
        }

        public ISettings Settings { get; }

        public Inventory Inventory { get; private set; }

        public Client Client { get; private set; }

        public GetPlayerResponse Profile { get; set; }
        public Navigation Navigation { get; private set; }

        public ILogicSettings LogicSettings { get; }

        public ITranslation Translation { get; }

        public IEventDispatcher EventDispatcher { get; }

        public GeoCoordinate ForceMoveTo { get; set; }

        public void StartForceMove(double lat, double lng)
        {
            ForceMoveTo = new GeoCoordinate(lat, lng);
        }

        public IWebProxy Proxy
        {
            get
            {
                if (Settings.UseProxy)
                {
                    NetworkCredential proxyCreds = new NetworkCredential(
                        Settings.ProxyLogin,
                        Settings.ProxyPass
                    );
                    WebProxy prox = new WebProxy(Settings.ProxyUri)
                    {
                        UseDefaultCredentials = false,
                        Credentials = proxyCreds,
                    };
                    return prox;
                }
                return null;
            }

        }

        public void Reset(ISettings settings, ILogicSettings logicSettings)
        {
            ApiFailureStrategy _apiStrategy = new ApiFailureStrategy(this);
            Client = new Client(Settings, _apiStrategy);
            // ferox wants us to set this manually
            Inventory = new Inventory(Client, logicSettings);
            Navigation = new Navigation(Client);
        }
    }
}