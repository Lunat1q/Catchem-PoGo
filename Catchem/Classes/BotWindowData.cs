using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Catchem.Extensions;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System.Net.Http;
using PoGo.PokeMobBot.Logic.Logging;
using System.Net;
using System.Windows;

namespace Catchem.Classes
{
    public class BotWindowData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public string Errors => _errosCount == 0 ? "" : _errosCount.ToString();
        private readonly Random _rnd = new Random();
        public Session Session;
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
        public readonly StateMachine Machine;
        public readonly Statistics Stats;
        public readonly StatisticsAggregator Aggregator;
        public readonly WpfEventListener Listener;
        public readonly ClientSettings Settings;
        public readonly LogicSettings Logic;
        public readonly GlobalSettings GlobalSettings;
        public int Coins;
        public TeamColor Team;
        public string PlayerName;
        public int MaxItemStorageSize;
        public int MaxPokemonStorageSize;
        public ObservableCollection<PokemonUiData> PokemonList = new ObservableCollection<PokemonUiData>();
        public ObservableCollection<ItemUiData> ItemList = new ObservableCollection<ItemUiData>();
        private readonly Queue<PointLatLng> _routePoints = new Queue<PointLatLng>();
        public readonly GMapRoute PlayerRoute;

        public ObservableCollection<PokemonId> PokemonsToEvolve => GlobalSettings.PokemonsToEvolve;
        public ObservableCollection<PokemonId> PokemonsNotToTransfer => GlobalSettings.PokemonsNotToTransfer;
        public ObservableCollection<PokemonId> PokemonsNotToCatch => GlobalSettings.PokemonsToIgnore;
        public ObservableCollection<PokemonId> PokemonToUseMasterball => GlobalSettings.PokemonToUseMasterball;
        public Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter => GlobalSettings.PokemonsTransferFilter;

        //public Label RunTime;
        private double _xpph;
        public bool Started;

        private readonly DispatcherTimer _timer;
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
                if (GlobalPlayerMarker != null)
                    GlobalPlayerMarker.Position = new PointLatLng(_lat, _lng);
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

        public TimeSpan Ts
        {
            get { return _ts; }
            set
            {
                _ts = value;
                OnPropertyChanged();
            }
        }

        public GMapRoute PathRoute { get; internal set; }

        public double LatStep, LngStep;
        internal int StartStarDust;

        public BotWindowData(string name, GlobalSettings gs, StateMachine sm, Statistics st, StatisticsAggregator sa, WpfEventListener wel, ClientSettings cs, LogicSettings l)
        {
            ProfileName = name;
            GlobalSettings = gs;
            Machine = sm;
            Stats = st;
            Aggregator = sa;
            Listener = wel;
            Settings = cs;
            Logic = l;
            Lat = gs.LocationSettings.DefaultLatitude;
            Lng = gs.LocationSettings.DefaultLongitude;

            Ts = new TimeSpan();
            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            _timer.Tick += delegate
            {
                Ts += new TimeSpan(0, 0, 1);
                _realWorkSec++;
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

        public void UpdateXppH()
        {
            if (Stats == null || Math.Abs(_ts.TotalHours) < 0.0000001)
                Xpph = 0;
            else
                Xpph = (Stats.TotalExperience / _ts.TotalHours);
        }

        public void PushNewRoutePoint(PointLatLng nextPoint)
        {
            _routePoints.Enqueue(nextPoint);
            if (_routePoints.Count > 100)
            {
                _routePoints.Dequeue();
                var latestPoint = PlayerRoute.Points.First();//Temp fix
                PlayerRoute.Points.Remove(latestPoint);
            }
            PlayerRoute.Points.Add(nextPoint);
        }

        private void WipeData()
        {
            Log = new List<Tuple<string, Color>>();
            MapMarkers = new Dictionary<string, GMapMarker>();
            MarkersQueue = new Queue<NewMapObject>();
            LogQueue = new Queue<Tuple<string, Color>>();
        }

        public void Stop(bool soft = false)
        {
            TimerStop();
            _cts.Cancel();
            WipeData();
            _ts = new TimeSpan();
            Started = false;
            ErrorsCount = 0;
            if (!soft)
                _realWorkSec = 0;
        }

        public async void Start()
        {
            if (Started) return;
            Started = true;
            _pauseCts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            if (!await CheckProxy())
            {
                Stop();
                return;
            }
            GlobalSettings.MapzenAPI.ApiKey = GlobalSettings.LocationSettings.MapzenApiElevationKey;
            ErrorsCount = 0;
            TimerStart();
            Session.Client.Player.SetCoordinates(GlobalSettings.LocationSettings.DefaultLatitude,
                GlobalSettings.LocationSettings.DefaultLongitude,
                GlobalSettings.LocationSettings.DefaultAltitude);
            Session.Client.Login = new PokemonGo.RocketAPI.Rpc.Login(Session.Client);
            LaunchBot();
        }

        private void LaunchBot()
        {
            Machine.AsyncStart(new VersionCheckState(), Session, CancellationToken);
            if (Session.LogicSettings.UseSnipeLocationServer)
                SnipePokemonTask.AsyncStart(Session, CancellationToken);
        }



        private WebProxy InitProxy()
        {
            if (!GlobalSettings.Auth.UseProxy) return null;
            var split = GlobalSettings.Auth.ProxyUri.Split(':');
            var prox = new WebProxy(split[0], int.Parse(split[1]))
            {
                Credentials = new NetworkCredential(GlobalSettings.Auth.ProxyLogin, GlobalSettings.Auth.ProxyPass),
                BypassProxyOnLocal = true
            };
            return prox;
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
                    var otherBot = MainWindow.BotsCollection.FirstOrDefault(x => x.GlobalSettings.Auth.ProxyUri.Equals(GlobalSettings.Auth.ProxyUri) && x.GlobalSettings.Auth.UseProxy && x.Started && x != this);
                    if (otherBot != null)
                    {
                        Logger.Write($"{otherBot.ProfileName} is already running with this proxy!", LogLevel.Info, ConsoleColor.Red, Session);
                    }
                    client = new HttpClient(new HttpClientHandler { Proxy = InitProxy() });
                    response = await client.GetAsync("http://ipv4bot.whatismyipaddress.com/", CancellationToken);
                    var proxiedIPres = await response.Content.ReadAsStringAsync();
                    var proxiedIp = proxiedIPres == null || proxiedIPres.Equals("<nil>") ? "INVALID PROXY" : proxiedIPres;
                    Logger.Write($"Your IP is: {unproxiedIp} / Proxy IP is: {proxiedIp}", LogLevel.Info, (unproxiedIp == proxiedIp) ? ConsoleColor.Red : ConsoleColor.Green, Session);
                    if (unproxiedIp == proxiedIp || proxiedIPres == null || proxiedIPres.Equals("<nil>"))
                    {
                        Logger.Write("Your ip match! I warned you!", LogLevel.Info, ConsoleColor.Red, Session);
                    }
                }
                else
                {
                    Logger.Write($"Your IP is: {unproxiedIp}", LogLevel.Info, ConsoleColor.Red, Session);
                }
            }
            catch (Exception)
            {
                Logger.Write("Proxy Down?", LogLevel.Info, ConsoleColor.Red, Session);
                return false;
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

        public async void BuildPokemonList(List<Tuple<PokemonData, double, int>> receivedList)
        {
            PokemonList.Clear();
            var pokemonFamilies = await Session.Inventory.GetPokemonFamilies();
            var pokemonSettings = (await Session.Inventory.GetPokemonSettings()).ToList();
            foreach (var pokemon in receivedList)
            {
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.Item1.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                var mon = new PokemonUiData(
                    pokemon.Item1.Id,
                    pokemon.Item1.PokemonId,
                    pokemon.Item1.PokemonId.ToInventorySource(),
                    (pokemon.Item1.Nickname == "" ? pokemon.Item1.PokemonId.ToString() : pokemon.Item1.Nickname),
                    pokemon.Item1.Cp,
                    pokemon.Item2,
                    family.FamilyId,
                    family.Candy_,
                    pokemon.Item1.CreationTimeMs);
                PokemonList.Add(mon);
                mon.UpdateTags(Logic);
            }
        }

        public void GotNewPokemon(ulong uid, PokemonId pokemonId, int cp, double iv, PokemonFamilyId family, int candy)
        {
            PokemonList.Add(new PokemonUiData(
                    uid,
                    pokemonId,
                    pokemonId.ToInventorySource(),
                    pokemonId.ToString(),
                    cp,
                    iv,
                    family,
                    candy,
                    (ulong)DateTime.UtcNow.ToUnixTime()));
            foreach (var pokemon in PokemonList.Where(x => x.Family == family))
            {
                pokemon.Candy = candy;
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
                    ItemList.Add(new ItemUiData(
                        item.Item1,
                        item.Item1.ToInventorySource(),
                        item.Item1.ToInventoryName(),
                        item.Item2));
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

        public void PokemonUpdated(ulong uid, int cp, double iv, PokemonFamilyId family, int candy)
        {
            var pokemonToUpdate = PokemonList.FirstOrDefault(x => x.Id == uid);
            if (pokemonToUpdate == null) return;
            pokemonToUpdate.Cp = cp;
            pokemonToUpdate.Iv = iv;
            foreach (var pokemon in PokemonList.Where(x => x.Family == family))
            {
                pokemon.Candy = candy;
            }
        }

        public async void CheckForMaxCatch()
        {
            try
            {
                if (!GlobalSettings.CatchSettings.PauseBotOnMaxHourlyRates || !(RealWorkH >= 1)) return;
                if (!(Stats?.TotalPokemons / RealWorkH > GlobalSettings.CatchSettings.MaxCatchPerHour) && !(Stats?.TotalPokestops / RealWorkH > GlobalSettings.CatchSettings.MaxPokestopsPerHour)) return;
                var stopSec = 10 * 60 + _rnd.Next(60 * 5);
                _realWorkSec += stopSec;
                var stopMs = stopSec * 1000;
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = $"Max amount of pokemos(or pokestops)/h reached, bot will be stoped for {(stopMs / (60000)).ToString("N1")} minutes"
                });
                Stop(true);
                _pauseCts.Dispose();
                _pauseCts = new CancellationTokenSource();
                await Task.Delay(stopMs, CancellationTokenPause);
                Start();
            }
            catch (OperationCanceledException)
            {
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "Bot pause routine canceled"
                });
            }
            catch (Exception)
            {
                Session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "Bot pause routine failed badly"
                });
            }
        }
    }
}
