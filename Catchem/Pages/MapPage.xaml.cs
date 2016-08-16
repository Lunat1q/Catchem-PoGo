using System;
using System.Threading;
using System.Threading.Tasks;
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

        private ISession CurSession => _bot.Session;

        private bool _followThePlayerMarker;
        private bool _keepPokemonsOnMap;
        public bool WindowClosing;
        private SettingsPage _botSettingsPage;
        private bool _loadingUi;

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
            pokeMap.MapProvider = GMap.NET.MapProviders.GMapProviders.OpenStreetMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            if (_bot != null)
                pokeMap.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            await Task.Delay(10);
        }

        private void pokeMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(pokeMap);
            //Getting real coordinates from mouse click
            var mapPos = pokeMap.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y);
            var lat = mapPos.Lat;
            var lng = mapPos.Lng;

            if (_bot == null) return;
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
                    addMarker(_bot.ForceMoveMarker);
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

        public void addMarker(GMapMarker marker)
        {
            pokeMap.Markers.Add(marker);
            MainWindow.BotWindow.GlobalMapView.addMarker(marker);
        }

        public void SetBot(BotWindowData bot)
        {
            _loadingUi = true;
            _bot = bot;
            LoadMarkersFromBot();
            if (_bot == null) return;
            pokeMap.Position = new PointLatLng(_bot._lat, _bot._lng);
            DrawPlayerMarker();
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
            _pathRoute.RegenerateShape(pokeMap);
            var path = _pathRoute.Shape as Path;
            if (path != null)
                path.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
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
            const int delay = 25;
            while (!WindowClosing)
            {
                if (_bot != null && _playerMarker != null && _bot.Started)
                {
                    if (_bot.MoveRequired && _bot.Started)
                    {
                        if (_bot.GotNewCoord && _bot.Started)
                        {
                            // ReSharper disable once PossibleLossOfFraction
                            _bot.LatStep = (_bot.Lat - _bot._lat) / (2000 / delay);
                            // ReSharper disable once PossibleLossOfFraction
                            _bot.LngStep = (_bot.Lng - _bot._lng) / (2000 / delay);
                            _bot.GotNewCoord = false;
                            pokeMap.UpdateLayout();
                            //BotSettingsPage.UpdateCoordBoxes();
                            _playerRoute.RegenerateShape(pokeMap);
                        }

                        _bot._lat += _bot.LatStep;
                        _bot._lng += _bot.LngStep;
                        _playerMarker.Position = new PointLatLng(_bot._lat, _bot._lng);
                        if (Math.Abs(_bot._lat - _bot.Lat) < 0.000000001 && Math.Abs(_bot._lng - _bot.Lng) < 0.000000001)
                            _bot.MoveRequired = false;
                        if (_followThePlayerMarker)
                            pokeMap.Position = new PointLatLng(_bot._lat, _bot._lng);
                    }
                }
                await Task.Delay(delay);
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
                                        Shape = Properties.Resources.pstop.ToImage("PokeStop"),
                                        Offset = new Point(-16, -32),
                                        ZIndex = 5
                                    };
                                    addMarker(marker);
                                    _bot.MapMarkers.Add(newMapObj.Uid, marker);
                                }
                                break;
                            case "ps_lured":
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                                    {
                                        Shape = Properties.Resources.pstop_lured.ToImage("Lured PokeStop"),
                                        Offset = new Point(-16, -32),
                                        ZIndex = 5
                                    };
                                    addMarker(marker);
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
            var pokemon = (PokemonId)Enum.Parse(typeof(PokemonId), newMapObj.OName);

            var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
            {
                Shape = pokemon.ToImage(),
                Offset = new Point(-15, -30),
                ZIndex = 10
            };
            addMarker(marker);
            _bot.MapMarkers.Add(newMapObj.Uid, marker);
        }

        private void DrawPlayerMarker()
        {
            if (_playerMarker == null)
            {
                _playerMarker = new GMapMarker(new PointLatLng(_bot._lat, _bot._lng))
                {
                    Shape = Properties.Resources.trainer.ToImage("Player"),
                    Offset = new Point(-14, -40),
                    ZIndex = 15
                };
                addMarker(_playerMarker);
                _playerRoute = _bot.PlayerRoute;
                addMarker(_playerRoute);
                _pathRoute = _bot.PathRoute;
                addMarker(_pathRoute);
            }
            else
            {
                _playerMarker.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            }
            if (_bot.ForceMoveMarker != null && !pokeMap.Markers.Contains(_bot.ForceMoveMarker))
                addMarker(_bot.ForceMoveMarker);
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
                if ((bool)box.IsChecked)
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
            if (box?.IsChecked != null) _followThePlayerMarker = (bool)box.IsChecked;
        }

        private void pokeMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            sl_mapZoom.Value = pokeMap.Zoom;
        }

        private void sl_mapZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null) return;
            pokeMap.Zoom = (int)sl.Value;
        }
    }
}
