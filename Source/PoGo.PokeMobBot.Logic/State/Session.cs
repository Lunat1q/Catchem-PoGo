#region using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using System.Net;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Tasks;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Data;
using POGOProtos.Data.Player;

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
        GeoCoordinate ForceMoveToResume { get; set; }
        MapzenAPI MapzenApi { get; set; }
        EggWalker EggWalker { get; set; }
        PlayerStats PlayerStats { get; }

        Statistics Stats { get; }

        RuntimeSettings Runtime { get; set; }

        ObservableCollection<ManualAction> ActionQueue { get; set; }

        ObservableCollection<PokeDexRecord> PokeDex { get; set; }

        PokemonData BuddyPokemon { get; set; }
        bool CaptchaChallenge { get; set; }
        long LastCaptchaRequest { get; set; }

        bool ForceMoveJustDone { get; set; }
        void StartForceMove(double lat, double lng);
        void AddActionToQueue(Func<Task<bool>> task, string name, ulong bindedPokeUid);
        void RemoveActionFromQueue(ManualAction action);
    }

    public enum BotState
    {
        Idle,
        Walk,
        FoundPokemons,
        Catch,
        Transfer,
        Battle,
        Evolve,
        LevelPoke,
        Renaming,
        Recycle,
        Busy,
        LuckyEgg,
        Paused,
        Captcha
    }


    public class Session : PropertyNotification, ISession
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
            Stats = new Statistics();
            EggWalker = new EggWalker(this);
            ActionQueue = new ObservableCollection<ManualAction>();
        }

        public void AddActionToQueue(Func<Task<bool>> task, string name, ulong bindedPokeUid)
        {
            if (task == null) return;
            var action = new ManualAction()
            {
                Action = task,
                Name = name,
                BindedPokeUid = bindedPokeUid,
                Uid = Guid.NewGuid().ToString(),
                Session = this
            };
            ActionQueue.Add(action);
        }

        public void RemoveActionFromQueue(string uid)
        {
            if (ActionQueue == null || ActionQueue.Count == 0) return;
            var action = ActionQueue.FirstOrDefault(x => x.Uid == uid);
            if (action != null)
            {
                RemoveFromManualActionQueue(action);
            }
        }

        private void RemoveFromManualActionQueue(ManualAction action)
        {
            if (action.BindedPokeUid > 0)
            {
                EventDispatcher.Send(new PokemonActionDoneEvent
                {
                    Uid = action.BindedPokeUid
                });
            }
            ActionQueue.Remove(action);
        }


        public void RemoveActionFromQueue(ManualAction action)
        {
            if (ActionQueue == null || ActionQueue.Count == 0) return;
            if (action != null)
            {
                RemoveFromManualActionQueue(action);
            }
        }

        private BotState _botState;

        public EggWalker EggWalker { get; set; }

        public bool CaptchaChallenge { get; set; }

        public BotState State
        {
            get
            {
                return _botState;
            }
            set
            {
                _botState = value;
                OnPropertyChanged();
            }
        }

        public PokemonData BuddyPokemon { get; set; }

        public ObservableCollection<PokeDexRecord> PokeDex
        {
            get { return Inventory.PokeDex; }
            set
            {
                Inventory.PokeDex = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ManualAction> ActionQueue { get; set; }

        public PlayerStats PlayerStats => Inventory.PlayerStats;

        public ISettings Settings { get; }
        public long LastCaptchaRequest { get; set; }

        public Statistics Stats { get; set; }

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
        public GeoCoordinate ForceMoveToResume { get; set; }

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
            Inventory = new Inventory(Client, logicSettings, Translation);
            Navigation = new HumanNavigation(Client);
            MapCache = new MapCache();
        }
    }
}