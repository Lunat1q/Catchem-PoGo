using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;

namespace Catchem
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

        public Session Session;
        private CancellationTokenSource _cts;
        public CancellationToken CancellationToken => _cts.Token;
        internal GMapMarker ForceMoveMarker;
        public List<Tuple<string, Color>> Log = new List<Tuple<string, Color>>();
        public Queue<Tuple<string, Color>> LogQueue = new Queue<Tuple<string, Color>>();
        public Dictionary<string, GMapMarker> MapMarkers = new Dictionary<string, GMapMarker>();
        public Queue<NewMapObject> MarkersQueue = new Queue<NewMapObject>();
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

        public TimeSpan Ts
        {
            get { return _ts; }
            set
            {
                _ts = value;
                OnPropertyChanged();
            }
        }

        public double LatStep, LngStep;

        public BotWindowData(string name, GlobalSettings gs, StateMachine sm, Statistics st, StatisticsAggregator sa, WpfEventListener wel, ClientSettings cs, LogicSettings l)
        {
            ProfileName = name;
            Settings = new ClientSettings(gs);
            Logic = new LogicSettings(gs);
            GlobalSettings = gs;
            Machine = sm;
            Stats = st;
            Aggregator = sa;
            Listener = wel;
            Settings = cs;
            Logic = l;

            Ts = new TimeSpan();
            _timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 1) };
            _timer.Tick += delegate
            {
                Ts += new TimeSpan(0, 0, 1);
            };
            _cts = new CancellationTokenSource();
            PlayerRoute = new GMapRoute(_routePoints);
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
                var point = _routePoints.Dequeue();
                PlayerRoute.Points.Remove(point);
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

        public void Stop()
        {
            TimerStop();
            _cts.Cancel();
            WipeData();
            _ts = new TimeSpan();
            Started = false;
            ErrorsCount = 0;
        }

        public void Start()
        {
            if (Started) return;
            ErrorsCount = 0;
            TimerStart();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            Started = true;
            Session.Client.Player.SetCoordinates(GlobalSettings.LocationSettings.DefaultLatitude,
                GlobalSettings.LocationSettings.DefaultLongitude,
                GlobalSettings.LocationSettings.DefaultAltitude);
            Session.Client.Login = new PokemonGo.RocketAPI.Rpc.Login(Session.Client);
            Machine.AsyncStart(new VersionCheckState(), Session, CancellationToken);
            if (Session.LogicSettings.UseSnipeLocationServer)
                SnipePokemonTask.AsyncStart(Session, CancellationToken);
        }

        private void TimerStart() => _timer?.Start();

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
    }
}
