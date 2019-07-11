using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Catchem.Extensions;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System.Net.Http;
using PoGo.PokeMobBot.Logic.Logging;
using System.Net;
using System.Windows;
using Catchem.UiTranslation;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Utils;
using static System.String;

namespace Catchem.Classes
{
    public class BotWindowData : CatchemNotified
    {
        public string ProfileName { get; set; }
        private int _errosCount;

        public int ErrorsCount
        {
            get { return _errosCount; }
            set
            {
                _errosCount = value;
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged("Errors");
            }
        }

        private bool _paused;

        private bool _expandedPanel;

        public bool ExpandedPanel
        {
            get
            {
                return _expandedPanel;
            }
            set
            {
                _expandedPanel = value;
                OnPropertyChanged();
            }
        }

        public string Errors => _errosCount == 0 ? "" : _errosCount.ToString();
        private readonly Random _rnd = new Random();
        public Session Session { get; set; }
        private CancellationTokenSource _cts;
        public CancellationToken CancellationToken => _cts.Token;
        private CancellationTokenSource _pauseCts;
        public CancellationToken CancellationTokenPause => _pauseCts.Token;
        internal GMapMarker ForceMoveMarker;
        public List<Tuple<string, Color>> Log = new List<Tuple<string, Color>>();
        public Queue<Tuple<string, Color>> LogQueue = new Queue<Tuple<string, Color>>();
        public Dictionary<string, GMapMarker> MapMarkers = new Dictionary<string, GMapMarker>();
        public GMapMarker GlobalPlayerMarker;
        public Queue<NewMapObject> MarkersQueue = new Queue<NewMapObject>();
        public Queue<NewMapObject> MarkersDelayRemove = new Queue<NewMapObject>();
        public GMapRoute PathRoute { get; internal set; }
        public readonly StateMachine Machine;
        public readonly ClientSettings Settings;
        public readonly LogicSettings Logic;
        public readonly GlobalSettings GlobalSettings;
        private int _level;
        private int _starDust;
        public int Coins;
        public TeamColor Team;
        public string PlayerName;
        public int MaxItemStorageSize;
        public int MaxPokemonStorageSize;
        public ObservableCollection<PokemonUiData> PokemonList = new ObservableCollection<PokemonUiData>();
        public ObservableCollection<ItemUiData> ItemList = new ObservableCollection<ItemUiData>();
        public ObservableCollection<ItemUiData> UsedItemsList = new ObservableCollection<ItemUiData>();
        public ObservableCollection<PokeEgg> EggList => Session.EggWalker?.Eggs;
        public ObservableCollection<ManualAction> ActionList => Session.ActionQueue;
        public ObservableCollection<PokeDexRecord> PokeDex => Session.PokeDex;

        private readonly Queue<PointLatLng> _routePoints = new Queue<PointLatLng>();
        public readonly GMapRoute PlayerRoute;

        public ObservableCollection<PokemonId> PokemonsToEvolve => GlobalSettings.PokemonsToEvolve;
        public ObservableCollection<PokemonId> PokemonsNotToTransfer => GlobalSettings.PokemonsNotToTransfer;
        public ObservableCollection<PokemonId> PokemonsNotToCatch => GlobalSettings.PokemonsToIgnore;
        public ObservableCollection<PokemonId> PokemonToUseMasterball => GlobalSettings.PokemonToUseMasterball;
        public ObservableCollection<TransferFilter> PokemonsTransferFilters => GlobalSettings.PokemonsTransferFilters;

        //public Label RunTime;
        private double _xpph;
        private bool _started;

        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _pauseTimer;
        private TimeSpan _ts;

        public double Lat;
        public double Lng;
        public bool GotNewCoord;
        public bool MoveRequired;
        private double _la, _ln;

        // ReSharper disable once InconsistentNaming
        internal double _lat
        {
            get { return _la; }
            set
            {
                GlobalSettings.LocationSettings.DefaultLatitude = value;
                _la = value;                
            }
        }

        // ReSharper disable once InconsistentNaming
        internal double _lng
        {
            get { return _ln; }
            set
            {
                GlobalSettings.LocationSettings.DefaultLongitude = value;
                _ln = value;
                var map = GlobalPlayerMarker?.Map;
                map?.Dispatcher.Invoke(new ThreadStart(delegate
                {
                    GlobalPlayerMarker.Position =
                        new PointLatLng(_lat, _lng);
                }));
            }
        }

        public double Xpph
        {
            get { return _xpph; }
            set
            {
                _xpph = value;
                OnPropertyChanged();
            }
        }

        private float _realWorkSec;

        private float RealWorkH => _realWorkSec/(60*60);

        public double PokemonsRate
        {
            get
            {
                if (Session?.Stats == null || RealWorkH < 0.001) return 0;
                return Session.Stats.TotalPokemons/RealWorkH;
            }
        }
        public double PokestopsRate
        {
            get
            {
                if (Session?.Stats == null || RealWorkH < 0.001) return 0;
                return Session.Stats.TotalPokestops / RealWorkH;
            }
        }

        public double StardustRate
        {
            get
            {
                if (Session?.Stats == null || RealWorkH < 0.001 || Session.Stats.TotalStardust == 0) return 0;
                return (Session.Stats.TotalStardust - StartStarDust) / RealWorkH;
            }
        }

        public TimeSpan Ts
        {
            get { return _ts; }
            set
            {
                _ts = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan PauseTs
        {
            get { return _pauseTs; }
            set
            {
                _pauseTs = value;
                OnPropertyChanged();
            }
        }

        public string LevelText => TranslationEngine.GetDynamicTranslationString("%LEVEL%", "Level") + ": " + Level;
        public int Level
        {
            get { return _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
                OnPropertyChangedByName("LevelText");
            }
        }

        public int StarDust
        {
            get { return _starDust; }
            set
            {
                _starDust = value;
                OnPropertyChanged();
            }
        }

        public bool Started
        {
            get { return _started; }
            set
            {
                _started = value;
                OnPropertyChanged(); 
            }
        }

        public bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;
                OnPropertyChanged();
                if (_paused == false && _pauseTimer.IsEnabled)
                {
                    _pauseTimer.Stop();
                }
            }
        }

        public double LatStep, LngStep;
        internal int StartStarDust;
        private TimeSpan _pauseTs;

        public BotWindowData(string name, GlobalSettings gs, StateMachine sm, ClientSettings cs, LogicSettings l)
        {
            ProfileName = name;
            GlobalSettings = gs;
            Machine = sm;
            Settings = cs;
            Logic = l;
            Lat = gs.LocationSettings.DefaultLatitude;
            Lng = gs.LocationSettings.DefaultLongitude;

            Ts = new TimeSpan();
            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            _pauseTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            _timer.Tick += delegate
            {
                Ts += new TimeSpan(0, 0, 1);
                _realWorkSec++;
            };
            _pauseTimer.Tick += delegate
            {
                PauseTs -= new TimeSpan(0, 0, 1);
            };


            _cts = new CancellationTokenSource();
            _pauseCts = new CancellationTokenSource();
            PlayerRoute = new GMapRoute(_routePoints);
            PathRoute = new GMapRoute(new List<PointLatLng>());

            GlobalPlayerMarker = new GMapMarker(new PointLatLng(Lat, Lng))
            {
                Shape = Properties.Resources.trainer.ToImage("Player - " + ProfileName),
                Offset = new Point(-14, -40),
                ZIndex = 15
            };
        }

        public void UpdateRunTime()
        {
            if (Session?.Stats == null || Math.Abs(RealWorkH) < 0.0000001)
                Xpph = 0;
            else
                Xpph = Session.Stats.TotalExperience / RealWorkH;

            if (Session?.Stats?.ExportStats != null)
            {
                Level = Session.Stats.ExportStats.Level;
                Session.Runtime.CurrentLevel = Level;
            }
        }

        public void PushNewRoutePoint(PointLatLng nextPoint)
        {
            _routePoints.Enqueue(nextPoint);
            if (_routePoints.Count > 100)
            {
                _routePoints.Dequeue();
                var latestPoint = PlayerRoute.Points.First(); //Temp fix
                PlayerRoute.Points.Remove(latestPoint);
            }
            PlayerRoute.Points.Add(nextPoint);

            PlayerRoute.Map?.Dispatcher.Invoke(new ThreadStart(
                delegate
                {
                    try
                    {
                        PlayerRoute.RegenerateShape(PlayerRoute.Map);
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("[NEWROUTE POINT ERROR] " + ex.Message + " trace: " + ex.StackTrace);
                    }
                }
                ));

        }

        private void WipeData()
        {
            try
            {
                Log = new List<Tuple<string, Color>>();
                MarkersQueue = new Queue<NewMapObject>();
                MapMarkers = new Dictionary<string, GMapMarker>();
                LogQueue = new Queue<Tuple<string, Color>>();
                PathRoute.Points.Clear();
                PathRoute.RegenerateShape(null);
                _routePoints.Clear();
                PlayerRoute.Points.Clear();
                PlayerRoute.RegenerateShape(null);
            }
            catch (Exception ex)
            {
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "Error during wiping bot data!"
                });
                Logger.Write($"[WIPE FAIL] Error: {ex.Message}", LogLevel.Error);
            }
        }

        public void Stop(bool soft = false)
        {
            TimerStop();
            _cts.Cancel();
            WipeData();
            _ts = new TimeSpan();
            Started = false;
            Session.State = BotState.Idle;
            ErrorsCount = 0;
            Session.Client.Login.UpdateHash();
            if (ForceMoveMarker != null && !soft)
            {
                ForceMoveMarker?.Map?.Dispatcher?.BeginInvoke(new ThreadStart(delegate
                {
                    ForceMoveMarker?.Map?.Markers?.Remove(ForceMoveMarker);
                }));
                ForceMoveMarker = null;
            }
            if (soft) return;
            _realWorkSec = 0;
            Session.ActionQueue.Clear();
            Session.ForceMoveTo = null;
            Session.ForceMoveToResume = null;
            if (Session?.Stats == null) return;
            Session.Stats.TotalPokemons = 0;
            Session.Stats.TotalPokestops = 0;
            Session.Stats.TotalExperience = 0;
        }

        public async void Start()
        {
            if (_started) return;
            AutoPauseCheck();
            PrepareMapData();
            Started = true;
            _pauseCts.Cancel();
            TokenReset();
            if (!await CheckProxy())
            {
                Stop();
                return;
            }
            ResetSession();
            TimerStart();
            LaunchBot();

            if (Paused)
                Paused = false;
        }

        private void TokenReset()
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void ResetSession()
        {
            GlobalSettings.MapzenAPI.ApiKey = GlobalSettings.LocationSettings.MapzenApiKey;
            ErrorsCount = 0;
            Session.LastCaptchaRequest = 0;
            Session.State = BotState.Idle;
            Session.CaptchaChallenge = false;

            Session.Client.Player.SetInitial(GlobalSettings.LocationSettings.DefaultLatitude,
               GlobalSettings.LocationSettings.DefaultLongitude,
               GlobalSettings.LocationSettings.DefaultAltitude);
            Session.Client.Login = new PokemonGo.RocketAPI.Rpc.Login(Session.Client);
            if (Session.Translation.CurrentCode != GlobalSettings.StartUpSettings.TranslationLanguageCode)
            {
                Session.Translation = Translation.Load(Logic);
            }
        }

        private void PrepareMapData()
        {
            Lat = _lat = Session.Settings.DefaultLatitude;
            Lng = _lng = Session.Settings.DefaultLongitude;
        }

        private void LaunchBot()
        {
            Session.State = BotState.Idle;
            Machine.AsyncStart(new VersionCheckState(), Session, CancellationToken);
            if (Session.LogicSettings.UseSnipeLocationServer)
                SnipePokemonTask.AsyncStart(Session, CancellationToken);
        }

        private async Task<bool> CheckProxy()
        {
            try
            {
                var defProxy = WebRequest.GetSystemWebProxy();
                defProxy.Credentials = CredentialCache.DefaultCredentials;
                var client = new HttpClient(new HttpClientHandler { Proxy = defProxy });
                var response = await client.GetAsync("http://ipv4bot.whatismyipaddress.com/", CancellationToken);
                var unproxiedIp = await response.Content.ReadAsStringAsync();
                if (GlobalSettings.Auth.UseProxy)
                {
                    var otherBot = MainWindow.BotsCollection.FirstOrDefault(x => x.GlobalSettings.Auth.UseProxy && x.GlobalSettings.Auth.ProxyUri.Equals(GlobalSettings.Auth.ProxyUri)  && x._started && x != this);
                    if (otherBot != null)
                    {
                        Logger.Write(Session.Translation.GetTranslation(TranslationString.ProxyAlready, otherBot.ProfileName), LogLevel.Info, ConsoleColor.Red, Session);
                    }
                    client = new HttpClient(new HttpClientHandler { Proxy = Session.Proxy });
                    response = await client.GetAsync("http://ipv4bot.whatismyipaddress.com/", CancellationToken);
                    var proxiedIPres = await response.Content.ReadAsStringAsync();
                    var proxiedIp = proxiedIPres == null || proxiedIPres.Equals("<nil>") ? "INVALID PROXY" : proxiedIPres;
                    Logger.Write(Session.Translation.GetTranslation(TranslationString.YourIpProxyIp, unproxiedIp, proxiedIp), LogLevel.Info, unproxiedIp == proxiedIp ? ConsoleColor.Red : ConsoleColor.Green, Session);
                    if (unproxiedIp == proxiedIp || proxiedIPres == null || proxiedIPres.Equals("<nil>"))
                    {
                        Logger.Write(Session.Translation.GetTranslation(TranslationString.IpMatch), LogLevel.Info, ConsoleColor.Red, Session);
                    }
                }
                else
                {
                    Logger.Write(Session.Translation.GetTranslation(TranslationString.YourIpIs,unproxiedIp), LogLevel.Info, ConsoleColor.Red, Session);
                }
            }
            catch (Exception ex)
            {
                Logger.Write(Session.Translation.GetTranslation(TranslationString.ProxyDown), LogLevel.Info, ConsoleColor.Red, Session);
                Logger.Write("Proxy Down error: " + ex.Message, LogLevel.Debug);
            }
            return true;
        }


        private void TimerStart() => _timer?.Start();

        internal void PushNewPathRoute(List<Tuple<double, double>> list)
        {
            PathRoute.Points.Clear();
            foreach (var item in list)
            {
                PathRoute.Points.Add(new PointLatLng(item.Item1, item.Item2));
            }         
        }

        private void TimerStop() => _timer?.Stop();

        internal void EnqueData()
        {
            while (LogQueue.Count > 0)
                Log.Add(LogQueue.Dequeue());
            foreach (var item in Log)
            {
                LogQueue.Enqueue(item);
                if (LogQueue.Count > 100)
                    LogQueue.Dequeue();
            }
            Log = new List<Tuple<string, Color>>();
            _routePoints.Clear();
        }

        public async void BuildPokemonList(List<PokemonData> receivedList)
        {
            var pokeInAction = PokemonList.Where(x => x != null && x.InAction).ToList();
            PokemonList.Clear();
            var pokemonFamilies = await Session.Inventory.GetPokemonFamilies();
            var pokemonSettings = (await Session.Inventory.GetPokemonSettings()).ToList();
            foreach (var pokemonGroup in receivedList.GroupBy(x=>x.PokemonId))
            {
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemonGroup.Key);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                foreach (var pokemon in pokemonGroup)
                {
                    if (PokemonList.Any(x => x.Id == pokemon.Id)) continue;

                    var mon = new PokemonUiData(
                        this,
                        pokemon.Id,
                        pokemon.PokemonId,
                        //pokemon.Item1.PokemonId.ToInventorySource(),
                        IsNullOrEmpty(pokemon.Nickname)
                            ? Session.Translation.GetPokemonName(pokemon.PokemonId)
                            : pokemon.Nickname,
                        pokemon.Cp,
                        pokemon.CalculatePokemonPerfection(),
                        family.FamilyId,
                        family.Candy_,
                        pokemon.CreationTimeMs,
                        pokemon.Favorite == 1,
                        !IsNullOrEmpty(pokemon.DeployedFortId),
                        pokemon.GetLevel(),
                        pokemon.Move1,
                        pokemon.Move2,
                        setting.Type,
                        setting.Type2,
                        (int) PokemonInfo.GetMaxCpAtTrainerLevel(pokemon, Level),
                        PokemonInfo.GetBaseStats(pokemon.PokemonId),
                        pokemon.Stamina,
                        pokemon.IndividualStamina,
                        (int) PokemonInfo.GetMaxCpAtTrainerLevel(pokemon, 40),
                        setting.CandyToEvolve,
                        pokemon.IndividualAttack,
                        pokemon.IndividualDefense,
                        pokemon.CpMultiplier + pokemon.AdditionalCpMultiplier,
                        pokemon.WeightKg,
                        pokemon.StaminaMax,
                        setting.EvolutionIds.ToArray(),
                        Session.Profile?.PlayerData?.BuddyPokemon?.Id == pokemon.Id);
                    if (pokeInAction.Any(x => x.Id == mon.Id))
                    {
                        mon.InAction = true;
                    }
                    PokemonList.Add(mon);
                    mon.UpdateTags(Logic);
                }
            }
        }

        public void GotNewPokemon(BaseNewPokemonEvent evt, string captureType)
        {
            if (PokemonList.Any(x => x.Id == evt.Uid)) return;
            PokemonList.Add(new PokemonUiData(
                    this,
                    evt.Uid,
                    evt.Id,
                    Session.Translation.GetPokemonName(evt.Id),
                    evt.Cp,
                    evt.Perfection,
                    evt.Family,
                    evt.Candy,
                    (ulong)DateTime.UtcNow.ToUnixTime(),
                    false,
                    false,
                    evt.Level,
                    evt.Move1,
                    evt.Move2, 
                    evt.Type1,
                    evt.Type2,
                    evt.MaxCp,
                    PokemonInfo.GetBaseStats(evt.Id),
                    evt.Stamina,
                    evt.IvSta,
                    evt.PossibleCp,
                    evt.CandyToEvolve,
                    evt.IvAtk,
                    evt.IvDef,
                    evt.Cpm,
                    evt.Weight,
                    evt.StaminaMax,
                    evt.Evolutions,
                    false));
            foreach (var pokemon in PokemonList.Where(x => x.Family == evt.Family))
            {
                pokemon.Candy = evt.Candy;
                pokemon.UpdateTags(Logic);
            }
        }

        public void LostPokemon(ulong uid, PokemonFamilyId? family, int? candy)
        {
            var targetPokemon = PokemonList.FirstOrDefault(x => x.Id == uid);
            if (targetPokemon == null) return;
            PokemonList.Remove(targetPokemon);
            if (family != null && candy != null)
            {
                foreach (var pokemon in PokemonList.Where(x => x.Family == family))
                {
                    pokemon.Candy = (int)candy;
                }
            }
        }

        public void NewPlayerData(string name, int invSize, int pokeSize, TeamColor team, int stardust, int coins)
        {
            PlayerName = name;
            MaxItemStorageSize = invSize;
            MaxPokemonStorageSize = pokeSize;
            Team = team;
            StartStarDust = stardust;
            Coins = coins;
        }

        public void GotNewItems(List<Tuple<ItemId, int>> newItems)
        {
            if (newItems == null) return;
            foreach (var item in newItems)
            {
                var targetItem = ItemList.FirstOrDefault(x => x.Id == item.Item1);
                if (targetItem != null)
                    targetItem.Amount += item.Item2;
                else
                {
                    ItemList.Add(new ItemUiData(
                        item.Item1,
                        item.Item1.ToInventoryName(),
                        item.Item2,
                        this));
                }
            }
        }

        public void LostItem(ItemId? item, int? amount)
        {
            if (item == null || amount == null) return;
            var targetItem = ItemList.FirstOrDefault(x => x.Id == item);
            if (targetItem == null) return;
            if (targetItem.Amount <= amount)
                ItemList.Remove(targetItem);
            else
                targetItem.Amount -= (int)amount;
        }

        public void UsedItem(ItemId? item, long? expire)
        {
            if (item == null || expire == null) return;
            var targetItem = ItemList.FirstOrDefault(x => x.Id == item);
            CreateUsedItem(item.Value, expire.Value);
            if (targetItem == null) return;
            var timeLeft = expire - DateTime.UtcNow.ToUnixTime();
            targetItem.SetActive((int)timeLeft);
        }

        public void CreateUsedItem(ItemId item, long expire)
        {
            var targetItem = UsedItemsList.FirstOrDefault(x => x.Id == item);
            var timeLeft = expire - DateTime.UtcNow.ToUnixTime();
            if (targetItem == null)
            {
                var used = new ItemUiData(item, "", 1, this);
                used.SetActive((int)timeLeft);
                UsedItemsList.Add(used);
            }
            else
            {
                targetItem.SetActive((int)timeLeft);
            }
        }

        public void PokemonActionDone(ulong uid)
        {
            var pokemonToUpdate = PokemonList.FirstOrDefault(x => x.Id == uid);
            if (pokemonToUpdate == null) return;
            pokemonToUpdate.InAction = false;
        }

        public void PokemonUpdated(ulong uid, int cp, double iv, PokemonFamilyId family, int candy, bool favourite, string name, int maxCp, int ivAtk, int ivDef, float cpm, float weight, double level, int stamina, int staminaMax)
        {
            var pokemonToUpdate = PokemonList.FirstOrDefault(x => x.Id == uid);
            if (pokemonToUpdate == null) return;
            pokemonToUpdate.Cp = cp;
            pokemonToUpdate.Iv = iv;
            pokemonToUpdate.MaxCp = maxCp;
            pokemonToUpdate.Favoured = favourite;
            pokemonToUpdate.Name = name;
            pokemonToUpdate.IvAtk = ivAtk;
            pokemonToUpdate.IvDef = ivDef;
            pokemonToUpdate.Cpm = cpm;
            pokemonToUpdate.Level = level;
            pokemonToUpdate.Weight = weight;
            pokemonToUpdate.Stamina = stamina;
            pokemonToUpdate.MaxStamina = staminaMax;
            foreach (var pokemon in PokemonList.Where(x => x.Family == family))
            {
                pokemon.Candy = candy;
            }
            
        }

        public void PokemonFavUpdated(ulong uid, bool favourite)
        {
            var pokemonToUpdate = PokemonList.FirstOrDefault(x => x.Id == uid);
            if (pokemonToUpdate == null) return;
            pokemonToUpdate.Favoured = favourite;
        }

        private void RandomizePosition()
        {
            GlobalSettings.LocationSettings.DefaultLatitude += _rnd.NextInRange(-0.0005, 0.0005);
            GlobalSettings.LocationSettings.DefaultLongitude += _rnd.NextInRange(-0.0003, 0.0003);
        }

        public async void AutoPauseCheck()
        {
            CheckForMaxCatch();
            await Task.Delay(500, default(CancellationToken));
            if (_nextCheckStamp < DateTime.UtcNow.ToUnixTime())
            {
                _nextCheckStamp = DateTime.UtcNow.AddMinutes(1).ToUnixTime();
                CheckForSchedule();
            }
            await Task.Delay(1000, default(CancellationToken));
            if (_nextActionStamp < DateTime.UtcNow.ToUnixTime())
            {
                _nextActionStamp = DateTime.UtcNow.AddMinutes(1).ToUnixTime();
                CheckForAction();
            }

        }

        public async void CheckForMaxCatch()
        {
            try
            {
                if (!GlobalSettings.CatchSettings.PauseBotOnMaxHourlyRates || 
                    RealWorkH < 1 || //DEBUG
                    Session?.Stats == null || Session.State == BotState.LuckyEgg || !Started || UsedItemsList.Any(x=>x.InUse)) return;

                var countXp = GlobalSettings.CatchSettings.MaxXPPerHour > 0;
                var countSd = GlobalSettings.CatchSettings.MaxStarDustPerHour > 0;
                var countPokemons = GlobalSettings.CatchSettings.MaxCatchPerHour > 0;
                var countPokestops = GlobalSettings.CatchSettings.MaxPokestopsPerHour > 0;


                var tooMuchPokemons = countPokemons && PokemonsRate > GlobalSettings.CatchSettings.MaxCatchPerHour;
                var tooMuchPokestops = countPokestops && PokestopsRate > GlobalSettings.CatchSettings.MaxPokestopsPerHour;
                var tooMuchXp = countXp && Xpph > GlobalSettings.CatchSettings.MaxXPPerHour;
                var tooMuchStarDust = countSd && StardustRate > GlobalSettings.CatchSettings.MaxStarDustPerHour;
                if (!tooMuchPokemons && !tooMuchPokestops && !tooMuchXp && !tooMuchStarDust) return;
                var pokemonSec = tooMuchPokemons
                    ? (PokemonsRate - GlobalSettings.CatchSettings.MaxCatchPerHour) / GlobalSettings.CatchSettings.MaxCatchPerHour * 60 * 60 : 0;
                var pokestopSec = tooMuchPokestops
                    ? (PokestopsRate - GlobalSettings.CatchSettings.MaxPokestopsPerHour) / GlobalSettings.CatchSettings.MaxPokestopsPerHour * 60 * 60 : 0;
                var xpSec = tooMuchXp
                    ? (Xpph - GlobalSettings.CatchSettings.MaxXPPerHour) / GlobalSettings.CatchSettings.MaxXPPerHour * 60 * 60 : 0;
                var stardustSec = tooMuchStarDust
                    ? (StardustRate - GlobalSettings.CatchSettings.MaxStarDustPerHour) / GlobalSettings.CatchSettings.MaxStarDustPerHour * 60 * 60 : 0;

                var stopSec = 10 * 60 + _rnd.Next(60 * 5) + (int)(new [] { pokestopSec, pokemonSec, xpSec, stardustSec }).Max();
                var stopMs = stopSec * 1000;

//#if DEBUG
//                stopMs /= 100;
//#endif
                _realWorkSec += stopSec;
                Stop(true);
                _pauseCts.Dispose();
                _pauseCts = new CancellationTokenSource();

                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = Session.Translation.GetTranslation(TranslationString.HourRateReached,PokemonsRate.ToN1(),PokestopsRate.ToN1(),Xpph.ToN1(), StardustRate.ToN1(),(stopMs / 60000).ToString("N1"))
                });
                RandomizePosition();
                await SetPause(stopMs);
                Start();
            }
            catch (OperationCanceledException)
            {
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = Session.Translation.GetTranslation(TranslationString.BotPauseCancel)
                });
            }
            catch (Exception ex)
            {
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = Session.Translation.GetTranslation(TranslationString.BotPauseFail)
                });
                Logger.Write($"[PAUSE FAIL] Error: {ex.Message}", LogLevel.Error);
            }
        }

        public async Task SetPause(int stopMs)
        {
            Paused = true;
            Session.EventDispatcher.Send(new NoticeEvent
            {
                Message = Session.Translation.GetTranslation(TranslationString.UpdatesAt) + " https://github.com/Lunat1q/Catchem-PoGo " + Session.Translation.GetTranslation(TranslationString.DiscordLink) + " https://discord.me/Catchem"
            });
            Session.State = BotState.Paused;
            PauseTs = new TimeSpan(0, 0, 0, 0, stopMs);
            _pauseTimer.Start();
            await Task.Delay(stopMs, CancellationTokenPause);
            _pauseTimer.Stop();
            Paused = false;
        }

        private long _nextActionStamp;
        private int _lastH = -1;
        public void CheckForAction()
        {
            if (GlobalSettings.Schedule == null || !GlobalSettings.Schedule.UseSchedule || GlobalSettings.Schedule.ActionList == null) return;
            var currentH = DateTime.Now.Hour;
            if (_lastH == currentH) return;
            var currentDay = DateTime.Now.DayOfWeekToInt();
            _lastH = currentH;
            var currentAction =
                GlobalSettings.Schedule.ActionList.Where(x => x.Day == currentDay && x.Hour == currentH).ToList();
            foreach (var action in currentAction)
            {
                try
                {
                    if (action.ActionArgs == null || action.ActionArgs.Length == 0) continue;
                    switch (action.ActionType)
                    {
                        case ScheduleActionType.ChangeRoute:
                            var newRoute =
                                MainWindow.BotWindow.GlobalCatchemSettings.Routes.FirstOrDefault(
                                    x =>
                                        string.Equals(x.Name, action.ActionArgs[0],
                                            StringComparison.CurrentCultureIgnoreCase));
                            if (newRoute != null)
                            {
                                GlobalSettings.LocationSettings.CustomRoute = newRoute.Route;
                                GlobalSettings.LocationSettings.CustomRouteName = newRoute.Name;
                            }
                            break;
                        case ScheduleActionType.ChangeLocation:
                            if (!Started)
                            {
                                UiHandlers.SetValueByName("DefaultLatitude", action.ActionArgs[0],
                                    GlobalSettings.LocationSettings);
                                UiHandlers.SetValueByName("DefaultLongitude", action.ActionArgs[1],
                                    GlobalSettings.LocationSettings);
                            }
                            else
                            {
                                double lat, lng;
                                if (action.ActionArgs[0].GetVal(out lat) && action.ActionArgs[1].GetVal(out lng))
                                {
                                    if (ForceMoveMarker == null)
                                    {
                                        Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
                                        {
                                            ForceMoveMarker = new GMapMarker(new PointLatLng(lat, lng))
                                            {
                                                Shape = Properties.Resources.force_move.ToImage("Force Move To"),
                                                Offset = new Point(-24, -48),
                                                ZIndex = int.MaxValue
                                            };
                                        }));
                                    }
                                    else
                                    {
                                        ForceMoveMarker.Position = new PointLatLng(lat, lng);
                                    }
                                    Session.StartForceMove(lat, lng);
                                }
                            }
                            break;
                        case ScheduleActionType.ChangeSettings:
                            UiHandlers.SetValueByName(action.ActionArgs[0], action.ActionArgs[1], GlobalSettings);
                            break;
                    }
                    Session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = Session.Translation.GetTranslation(TranslationString.ScheduleAction, action.ActionType, action.Args)
                    });
                }
                catch (Exception ex)
                {
                    Session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = Session.Translation.GetTranslation(TranslationString.ScheduleActionFailed, action.ActionType, action.Args)
                    });
                    Logger.Write($"[ScheduleActionFAILED] Error: {ex.Message}");
                }
            }
        }


        private bool _awaitingToShutDown;
        private long _nextCheckStamp;

        public async void CheckForSchedule(bool starting = false)
        {
            if (GlobalSettings.Schedule == null || !GlobalSettings.Schedule.UseSchedule || !Started || _awaitingToShutDown) return;
            var currentH = DateTime.Now.Hour;
            var currentDay = DateTime.Now.DayOfWeekToInt();

            if (!GlobalSettings.Schedule.Schedule[currentDay, currentH])
            {
                _awaitingToShutDown = true;

                if (!await CheckBotStateTask.Execute(Session, default(CancellationToken), true))
                {
                    _awaitingToShutDown = false;
                    return;
                }
                _awaitingToShutDown = false;
                var timePassed = DateTime.Now.Minute*60 + DateTime.Now.Second;
                float hoursToStop;
                var turnOnDay = -1;
                var turnOnHour = -1;

                if (LookForTurnOnHour(currentH, currentDay, out hoursToStop, ref turnOnDay, ref turnOnHour))
                {
                    try
                    {
                        var extraMin = _rnd.Next(10);
                        hoursToStop -= (float) timePassed/3600;
                        var stopSec = hoursToStop*3600 + extraMin*60;

                        _realWorkSec += stopSec;
                        Stop(true);

                        RandomizePosition();
                        _pauseCts.Dispose();
                        _pauseCts = new CancellationTokenSource();

                        Session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = Session.Translation.GetTranslation(TranslationString.SchedulePause, hoursToStop.ToN1(), Extensions.Extensions.GetDayName(turnOnDay), turnOnHour.ToString("00"), extraMin.ToString("00"))
                        });
                        await SetPause((int) stopSec*1000);
                        Start();
                    }
                    catch (OperationCanceledException)
                    {
                        Session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = Session.Translation.GetTranslation(TranslationString.BotPauseCancel)
                        });
                    }
                    catch (Exception ex)
                    {
                        Session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = Session.Translation.GetTranslation(TranslationString.BotPauseFail)
                        });
                        Logger.Write($"[SCHEDULE FAIL] Error: {ex.Message}", LogLevel.Error);
                    }
                }
            }
        }


        private bool LookForTurnOnHour(int curH, int curD, out float hoursToStop, ref int turnOnDay, ref int turnOnHour)
        {
            bool found = false;
            bool failed = false;
            var initialH = curH;
            var initialD = curD;
            hoursToStop = 0;
            while (!found)
            {
                curH++;
                hoursToStop++;
                if (curH > 23)
                {
                    curH = 0;
                    curD++;
                }
                if (curD > 6)
                    curD = 0;

                if (curH == initialH && curD == initialD)
                {
                    failed = true;
                    break;
                }
                if (GlobalSettings.Schedule.Schedule[curD, curH])
                {
                    turnOnHour = curH;
                    turnOnDay = curD;
                    found = true;
                }
            }
            return !failed;
        }
    }
}
