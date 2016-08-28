#region using directives

using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using System.Net;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.PoGoUtils;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public interface ISession
    {
        ISettings Settings { get; }
        Inventory Inventory { get; }
        Client Client { get; }
        BotState State { get; set; }
        GetPlayerResponse Profile { get; set; }
        HumanNavigation Navigation { get; }
        MapCache MapCache { get; }
        ILogicSettings LogicSettings { get; }
        ITranslation Translation { get; }
        IEventDispatcher EventDispatcher { get; }
        IWebProxy Proxy { get; }
        GeoCoordinate ForceMoveTo { get; set; }
        MapzenAPI MapzenApi { get; set; }

        RuntimeSettings Runtime { get; set; }

        bool ForceMoveJustDone { get; set; }
        void StartForceMove(double lat, double lng);
    }

    public enum BotState
    {
        Idle,
        Walk,
        Catch,
        Transfer,
        Battle,
        Evolve,
        LevelPoke,
        Renaming,
        Recycle,
        Busy
    }


    public class Session : ISession
    {
        public Session(ISettings settings, ILogicSettings logicSettings)
        {
            Settings = settings;
            LogicSettings = logicSettings;
            ApiFailureStrategy = new ApiFailureStrategy(this);
            EventDispatcher = new EventDispatcher();
            Translation = Common.Translation.Load(logicSettings);
            Reset(settings, LogicSettings);
            Runtime = new RuntimeSettings();
            State = BotState.Idle;
        }

        public BotState State { get; set; }

        public ISettings Settings { get; }

        public Inventory Inventory { get; private set; }

        public Client Client { get; private set; }
        public MapzenAPI MapzenApi { get; set; }

        public RuntimeSettings Runtime { get; set; }

        public GetPlayerResponse Profile { get; set; }
        public HumanNavigation Navigation { get; private set; }

        public MapCache MapCache { get; private set; }
        public ILogicSettings LogicSettings { get; }

        public ITranslation Translation { get; set; }

        public IEventDispatcher EventDispatcher { get; }

		public ApiFailureStrategy ApiFailureStrategy { get; set; }

        public GeoCoordinate ForceMoveTo { get; set; }

        public void StartForceMove(double lat, double lng)
        {
            ForceMoveTo = new GeoCoordinate(lat, lng);
        }
        public bool ForceMoveJustDone { get; set; }

        public IWebProxy Proxy
        {
            get
            {
                if (!Settings.UseProxy) return null;
                NetworkCredential proxyCreds = null;
                if (Settings.ProxyLogin != "")
                    proxyCreds = new NetworkCredential(Settings.ProxyLogin, Settings.ProxyPass);
                var prox = new WebProxy(Settings.ProxyUri)
                {
                    UseDefaultCredentials = false,
                    Credentials = proxyCreds,
                };
                return prox;
            }

        }

        public void Reset(ISettings settings, ILogicSettings logicSettings)
        {
            Client = new Client(Settings, ApiFailureStrategy);
            // ferox wants us to set this manually
            Inventory = new Inventory(Client, logicSettings);
            Navigation = new HumanNavigation(Client);
            MapCache = new MapCache();
        }
    }
}