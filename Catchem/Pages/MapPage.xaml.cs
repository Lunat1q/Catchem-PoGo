using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Catchem.Classes;
using Catchem.Extensions;
using Catchem.Interfaces;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Enums;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for MapPage.xaml
    /// </summary>
    public partial class MapPage : IBotPage
    {
        private BotWindowData _bot;
        private GMapMarker _playerMarker;
        private GMapRoute _playerRoute;
        private GMapRoute _pathRoute;
        private GMapRoute _optPathRoute;

        private ISession CurSession => _bot.Session;

        private bool _followThePlayerMarker;
        private bool _keepPokemonsOnMap;
        public bool WindowClosing;
        private SettingsPage _botSettingsPage;
        private bool _loadingUi;
        public static int Delay = 25;
        private readonly PlayerMovement _playerMovement = new PlayerMovement();

        public MapPage()
        {
            InitializeComponent();
            InitializeMap();

            MovePlayer();
            MarkersWorker();
        }

        public void SetSettingsPage(SettingsPage page)
        {
            _botSettingsPage = page;
        }

        private async void InitializeMap()
        {
            pokeMap.Bearing = 0;
            pokeMap.CanDragMap = true;
            pokeMap.DragButton = MouseButton.Left;
            pokeMap.MaxZoom = 18;
            pokeMap.MinZoom = 2;
            pokeMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            pokeMap.IgnoreMarkerOnMouseWheel = true;
            pokeMap.ShowCenter = false;
            pokeMap.ShowTileGridLines = false;
            pokeMap.Zoom = 18;
            GMap.NET.MapProviders.GMapProvider.WebProxy = System.Net.WebRequest.GetSystemWebProxy();
            GMap.NET.MapProviders.GMapProvider.WebProxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            pokeMap.MapProvider = GMap.NET.MapProviders.GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            if (_bot != null)
                pokeMap.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            await Task.Delay(10);
        }

        private void pokeMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_bot == null) return;
            var mousePos = e.GetPosition(pokeMap);
            //Getting real coordinates from mouse click
            var mapPos = pokeMap.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y);
            var lat = mapPos.Lat;
            var lng = mapPos.Lng;

            //TEST ROUTING
            //var startLat = Math.Round(_bot.Lat, 2);
            //var startLng = Math.Round(_bot.Lng, 2);

            //var top = (startLat + 0.1).ToString(CultureInfo.InvariantCulture);
            //var bot = (startLat - 0.1).ToString(CultureInfo.InvariantCulture);
            //var left = (startLng - 0.1).ToString(CultureInfo.InvariantCulture);
            //var right = (startLng + 0.1).ToString(CultureInfo.InvariantCulture);

            //var routerDb = new RouterDb();
            //Router router = new Router(routerDb);
            //var apiRequest = (HttpWebRequest)WebRequest.Create($"http://overpass.osm.rambler.ru/cgi/xapi_meta?*[bbox={left},{bot},{right},{top}]");
            //apiRequest.Proxy = WebRequest.DefaultWebProxy;
            //apiRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
            //var res = (HttpWebResponse)apiRequest.GetResponse();
            //using (var urlStream = res.GetResponseStream())
            //{
            //    // create source stream.
            //    var source = new XmlOsmStreamSource(urlStream);

            //    routerDb.LoadOsmData(source, Vehicle.Pedestrian);
            //}
            //// calculate a route.
            //var route = router.Calculate(Vehicle.Pedestrian.Fastest(),
            //    (float)_bot.Lat, (float)_bot.Lng, (float)lat, (float)lng);
            //var geoJson = route.ToGeoJson();



            if (_bot.Started)
            {
                if (_bot.ForceMoveMarker == null)
                {
                    _bot.ForceMoveMarker = new GMapMarker(mapPos)
                    {
                        Shape = Properties.Resources.force_move.ToImage("Force Move To"),
                        Offset = new Point(-24, -48),
                        ZIndex = int.MaxValue
                    };
                    AddMarker(_bot.ForceMoveMarker);
                }
                else
                {
                    _bot.ForceMoveMarker.Position = mapPos;
                }
                CurSession.StartForceMove(lat, lng);
            }
            else
            {
                _bot.Lat = _bot._lat = lat;
                _bot.Lng = _bot._lng = lng;
                _bot.GlobalSettings.LocationSettings.DefaultLatitude = lat;
                _bot.GlobalSettings.LocationSettings.DefaultLongitude = lng;
                DrawPlayerMarker();
                _botSettingsPage.UpdateCoordBoxes();
            }
        }

        public void AddMarker(GMapMarker marker)
        {
            pokeMap.Markers.Add(marker);
            //MainWindow.BotWindow.GlobalMapView.addMarker(marker);
        }

        public void SetBot(BotWindowData bot)
        {
            _loadingUi = true;
            _bot = bot;
            LoadMarkersFromBot();
            if (_bot == null) return;
            pokeMap.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            DrawPlayerMarker();
            UpdatePathRoute();
            sl_moveSpeedFactor.Value = _bot.GlobalSettings.LocationSettings.MoveSpeedFactor;
            _loadingUi = false;
        }

        public void ClearData()
        {
            pokeMap.Markers.Clear();
            _playerMarker = null;
        }

        public void UpdatePathRoute()
        {
            _pathRoute?.RegenerateShape(pokeMap);
            var path = _pathRoute?.Shape as Path;
            if (path != null)
                path.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }

        public void UpdateOptPathRoute(List<Tuple<double, double>> list)
        {
            if (_optPathRoute == null)
                _optPathRoute = new GMapRoute(new List<PointLatLng>());
            _optPathRoute.Points.Clear();
            var points = list.Select(x => new PointLatLng(x.Item1, x.Item2));
            _optPathRoute.Points.AddRange(points);
            _optPathRoute.RegenerateShape(pokeMap);
            var path = _optPathRoute.Shape as Path;
            if (path != null)
                path.Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 39));
            if (!pokeMap.Markers.Contains(_optPathRoute))
                pokeMap.Markers.Add(_optPathRoute);
        }


        public void UpdateCurrentBotCoords(BotWindowData botReceiver)
        {
            botReceiver.MoveRequired = true;
            if (Math.Abs(botReceiver._lat) < 0.001 && Math.Abs(botReceiver._lng) < 0.001)
            {
                botReceiver._lat = botReceiver.Lat;
                botReceiver._lng = botReceiver.Lng;
                Dispatcher.BeginInvoke(
                    new ThreadStart(delegate { pokeMap.Position = new PointLatLng(botReceiver.Lat, botReceiver.Lng); }));
            }

            if (_playerMarker == null)
            {
                Dispatcher.BeginInvoke(new ThreadStart(DrawPlayerMarker));
            }
            else
            {
                botReceiver.GotNewCoord = true;
            }
        }
        #region Async Workers
        private async void MovePlayer()
        {
            while (!WindowClosing)
            {
                if (_bot != null && _playerMarker != null && _bot.Started)
                {
                    if (_bot.MoveRequired && _bot.Started)
                    {
                        if (_bot.GotNewCoord && _bot.Started)
                        {                            
                            _bot.GotNewCoord = false;
                            pokeMap.UpdateLayout();
                            //BotSettingsPage.UpdateCoordBoxes();
                            _playerRoute.RegenerateShape(pokeMap);
                            UpdateMarkerDirection();
                        }

                        _bot._lat += _bot.LatStep;
                        _bot._lng += _bot.LngStep;
                        _playerMarker.Position = new PointLatLng(_bot._lat, _bot._lng);
                        if (Math.Abs(_bot._lat - _bot.Lat) < 0.000000001 && Math.Abs(_bot._lng - _bot.Lng) < 0.000000001)
                        {
                            _bot.MoveRequired = false;
                            UpdateMarkerDirection();
                        }
                        if (_followThePlayerMarker)
                            pokeMap.Position = new PointLatLng(_bot._lat, _bot._lng);
                    }
                }
                await Task.Delay(Delay);
            }
        }

        private void UpdateMarkerDirection()
        {
            try
            {
                var direction = _playerMovement.CalcDirection(_bot.MoveRequired, _bot.LatStep, _bot.LngStep);
                if (_playerMarker.Tag == null || (MoveDirections) _playerMarker.Tag != direction)
                {
                    _playerMarker.Tag = direction;
                    _playerMarker.Shape = _playerMovement.GetCurrentImage(_bot.MoveRequired, _bot.LatStep, _bot.LngStep);
                    _playerMarker.Offset = new Point(-12, -36);
                    _playerMarker.ZIndex = 15;
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        
        private async void MarkersWorker()
        {
            while (!WindowClosing)
            {
                if (_bot?.MarkersQueue.Count > 0)
                {
                    try
                    {
                        var newMapObj = _bot.MarkersQueue.Dequeue();
                        switch (newMapObj.OType)
                        {
                            case "ps":
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                                    {
                                        Shape = Properties.Resources.pstop.ToImage("PokeStop"), Offset = new Point(-16, -32), ZIndex = 5
                                    };
                                    AddMarker(marker);
                                    _bot.MapMarkers.Add(newMapObj.Uid, marker);
                                }
                                break;
                            case "ps_lured":
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                                    {
                                        Shape = Properties.Resources.pstop_lured.ToImage("Lured PokeStop"), Offset = new Point(-16, -32), ZIndex = 5
                                    };
                                    AddMarker(marker);
                                    _bot.MapMarkers.Add(newMapObj.Uid, marker);
                                }
                                break;
                            case "pm_rm":
                                if (_bot.MapMarkers.ContainsKey(newMapObj.Uid) && !_keepPokemonsOnMap)
                                    RemoveMarker(newMapObj.Uid, _bot.MapMarkers[newMapObj.Uid]);
                                else
                                    _bot.MarkersDelayRemove.Enqueue(newMapObj);

                                break;
                            case "forcemove_done":
                                if (_bot.ForceMoveMarker != null)
                                {
                                    pokeMap.Markers.Remove(_bot.ForceMoveMarker);
                                    MainWindow.BotWindow.GlobalMapView.removeMarker(_bot.ForceMoveMarker);
                                    _bot.ForceMoveMarker = null;
                                }
                                break;
                            case "pm":
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    CreatePokemonMarker(newMapObj);
                                }
                                break;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                await Task.Delay(10);
            }
        }

        #endregion

        private void RemoveMarker(string uid, GMapMarker marker)
        {
            pokeMap.Markers.Remove(marker);
            MainWindow.BotWindow.GlobalMapView.removeMarker(marker);
            _bot.MapMarkers.Remove(uid);
        }

        private void CreatePokemonMarker(NewMapObject newMapObj)
        {
            var pokemon = (PokemonId) Enum.Parse(typeof(PokemonId), newMapObj.OName);

            var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
            {
                Shape = pokemon.ToImage(), Offset = new Point(-12, -30), ZIndex = 10
            };
            AddMarker(marker);
            _bot.MapMarkers.Add(newMapObj.Uid, marker);
        }

        private void DrawPlayerMarker()
        {
            if (_playerMarker == null)
            {
                _playerMarker = new GMapMarker(new PointLatLng(_bot._lat, _bot._lng))
                {
                    Shape = Properties.Resources.trainer.ToImage("Player"), Offset = new Point(-14, -40), ZIndex = 15
                };
                AddMarker(_playerMarker);
                _playerRoute = _bot.PlayerRoute;
                AddMarker(_playerRoute);
                _pathRoute = _bot.PathRoute;
                AddMarker(_pathRoute);
                pokeMap.UpdateLayout();
                pokeMap.Zoom--;
                pokeMap.Zoom++;
            }
            else
            {
                _playerMarker.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            }
            if (_bot.ForceMoveMarker != null && !pokeMap.Markers.Contains(_bot.ForceMoveMarker))
                AddMarker(_bot.ForceMoveMarker);
        }

        public void LoadMarkersFromBot()
        {
            foreach (var marker in _bot.MapMarkers.Values)
            {
                pokeMap.Markers.Add(marker);
            }
        }

        private void sl_moveSpeedFactor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null || l_moveSpeedFactor == null) return;
            l_moveSpeedFactor.Content = sl.Value;
            if (_bot == null || _loadingUi) return;
            _bot.GlobalSettings.LocationSettings.MoveSpeedFactor = sl.Value;
        }

        private void cb_keepPokemonMarkers_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box?.IsChecked != null)
            {
                if ((bool) box.IsChecked)
                {
                    _keepPokemonsOnMap = true;
                }
                else
                {
                    _keepPokemonsOnMap = false;
                    foreach (var bot in MainWindow.BotsCollection)
                        foreach (var item in bot.MarkersDelayRemove)
                            bot.MarkersQueue.Enqueue(item);
                }
            }
        }

        private void mapFollowThePlayer_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box?.IsChecked != null) _followThePlayerMarker = (bool) box.IsChecked;
        }

        private void pokeMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            sl_mapZoom.Value = pokeMap.Zoom;
        }

        private void sl_mapZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null) return;
            pokeMap.Zoom = (int) sl.Value;
        }
    }

    internal enum MoveDirections
    {
        Top,
        Down,
        Left,
        Right,
        Stay
    }
}
