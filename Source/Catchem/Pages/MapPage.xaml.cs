using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic.DataStorage;
using PoGo.PokeMobBot.Logic.Logging;
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

        //private bool _followThePlayerMarker;
        //private bool _keepPokemonsOnMap;
        public bool WindowClosing;
        private SettingsPage _botSettingsPage;
        private bool _loadingUi;
        public static int Delay = 25;
        private readonly PlayerMovement _playerMovement = new PlayerMovement();
        private CatchemSettings _globalSettings;

        public void SetGlobalSettings(CatchemSettings settings)
        {
            _globalSettings = settings;
            pokeMap.MapProvider = _globalSettings.Provider;
            MapProviderComboBox.SelectedItem = _globalSettings.ProviderEnum;
            cb_mapFollowThePlayer.IsChecked = _globalSettings.FollowTheWhiteRabbit;
            cb_keepPokemonMarkers.IsChecked = _globalSettings.KeepPokeMarkersOnMap;
            _globalSettings.BindNewMapProvider(provider =>
            {
                pokeMap.MapProvider = provider;
                return true;
            });
        }

        public void SetProvider(GMapProvider provider)
        {
            pokeMap.MapProvider = provider;
        }

        public MapPage()
        {
            InitializeComponent();
            InitializeMap();

            MovePlayer();
            Task.Run(MarkersWorker);
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
            GMapProvider.WebProxy = WebRequest.GetSystemWebProxy();
            GMapProvider.WebProxy.Credentials = CredentialCache.DefaultCredentials;
            pokeMap.MapProvider = GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            if (_bot != null)
                pokeMap.Position = new PointLatLng(_bot.Lat, _bot.Lng);

            MapProviderComboBox.ItemsSource = Enum.GetValues(typeof(MapProvider));


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
            Dispatcher.Invoke(new ThreadStart(LoadMarkersFromBot));
            if (_bot == null) return;
            pokeMap.Position = new PointLatLng(_bot.Lat, _bot.Lng);
            Dispatcher.Invoke(new ThreadStart(DrawPlayerMarker));
            Dispatcher.Invoke(new ThreadStart(UpdatePathRoute));
            sl_moveSpeedFactor.Value = _bot.GlobalSettings.LocationSettings.MoveSpeedFactor;
            _loadingUi = false;
        }

        public void ClearData()
        {
            pokeMap?.Markers?.Clear();
            _playerMarker = null;
        }

        public void UpdatePathRoute()
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                try
                {
                    _pathRoute?.RegenerateShape(pokeMap);
                }
                catch (Exception ex)
                {
                    Logger.Write("[UPDATE PATH ROUTE ERROR] " + ex.Message + " trace: " + ex.StackTrace);
                }
                var path = _pathRoute?.Shape as Path;
                if (path != null)
                    path.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }));
        }

        public void UpdateOptPathRoute(List<Tuple<double, double>> list)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                if (_optPathRoute == null)
                    _optPathRoute = new GMapRoute(new List<PointLatLng>());
                _optPathRoute.Points.Clear();
                if (list.Any())
                {
                    var points = list.Select(x => new PointLatLng(x.Item1, x.Item2));
                    _optPathRoute.Points.AddRange(points);
                }
                _optPathRoute.RegenerateShape(pokeMap);
                var path = _optPathRoute.Shape as Path;
                if (path != null)
                    path.Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 39));
                if (!pokeMap.Markers.Contains(_optPathRoute))
                    pokeMap.Markers.Add(_optPathRoute);
            }));
        }


        public void UpdateCurrentBotCoords(BotWindowData botReceiver)
        {
            botReceiver.MoveRequired = true;
            if (Math.Abs(botReceiver._lat) < 0.001 && Math.Abs(botReceiver._lng) < 0.001)
            {
                botReceiver._lat = botReceiver.Lat;
                botReceiver._lng = botReceiver.Lng;
                Dispatcher.Invoke(
                    new ThreadStart(delegate
                    {
                        pokeMap.Position = new PointLatLng(botReceiver.Lat, botReceiver.Lng);

                    }));
            }

            if (_playerMarker == null)
            {
                Dispatcher.Invoke(new ThreadStart(DrawPlayerMarker));
            }
            else
            {
                botReceiver.GotNewCoord = true;
            }
        }
        #region Async Workers

        private async void MovePlayer()
        {
            var lastDistance = double.MaxValue;
            while (!WindowClosing)
            {

                if (_bot != null && _playerMarker != null && _bot.Started)
                {
                    try
                    {
                        if (_bot.MoveRequired && _bot.Started)
                        {
                            if (_bot.GotNewCoord && _bot.Started )
                            {
                                _bot.GotNewCoord = false;
                                lastDistance = double.MaxValue;
                                UpdateMarkerDirection();
                            }
                            _bot._lat += _bot.LatStep;
                            _bot._lng += _bot.LngStep;

                            await pokeMap.Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                if (_playerMarker != null)
                                {
                                    _playerMarker.Position = new PointLatLng(_bot._lat, _bot._lng);
                                }
                            }));

                            var dist = LocationUtils.CalculateDistanceInMeters(_bot._lat, _bot._lng, _bot.Lat, _bot.Lng);
                            if (dist < 0.5 || lastDistance < dist)
                            {
                                _bot.MoveRequired = false;
                                UpdateMarkerDirection();
                            }
                            lastDistance = dist;
                            if (_globalSettings.FollowTheWhiteRabbit)
                            {
                                await pokeMap.Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    pokeMap.Position =
                                        new PointLatLng(
                                            _bot._lat, _bot._lng);
                                }));
                            }
                                //await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                //{
                                //    pokeMap.Position =
                                //        new PointLatLng(
                                //            _bot._lat, _bot._lng);
                                //}));
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Logger.Write("[MOVE PLAYER FAILURE] " + ex.Message + " trace: " + ex.StackTrace, LogLevel.Error);
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
                    pokeMap.Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        _playerMarker.Tag = direction;
                        _playerMarker.Shape = _playerMovement.GetCurrentImage(_bot.MoveRequired, _bot.LatStep,
                            _bot.LngStep);
                        _playerMarker.Offset = new Point(-12, -36);
                        _playerMarker.ZIndex = 15;
                    }));
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        
        private async Task MarkersWorker()
        {
            while (!WindowClosing)
            {
                if (_bot?.MarkersQueue.Count > 0)
                {
                    try
                    {
                        var newMapObj = _bot.MarkersQueue.Dequeue();
                        if (newMapObj == null) continue;
                        switch (newMapObj.OType)
                        {
                            case MapPbjectType.Pokestop:
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var name = DbHandler.GetPokeStopName(newMapObj.Uid);
                                    await CreatePokestopMarker(name, newMapObj);
                                }
                                break;
                            case MapPbjectType.PokestopLured:
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var name = DbHandler.GetPokeStopName(newMapObj.Uid);
                                    await CreatePokestopMarker(name, newMapObj);
                                }
                                break;
                            case MapPbjectType.PokemonRemove:
                                if (_bot.MapMarkers.ContainsKey(newMapObj.Uid) && !_globalSettings.KeepPokeMarkersOnMap)
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        RemoveMarker(newMapObj.Uid, _bot.MapMarkers[newMapObj.Uid]);
                                    }));
                                }
                                else
                                    _bot.MarkersDelayRemove.Enqueue(newMapObj);

                                break;
                            case MapPbjectType.ForceMoveDone:
                                if (_bot.ForceMoveMarker != null)
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        pokeMap.Markers.Remove(_bot.ForceMoveMarker);
                                        MainWindow.BotWindow.GlobalMapView.RemoveMarker(_bot.ForceMoveMarker);
                                        _bot.ForceMoveMarker = null;
                                    }));
                                }
                                break;
                            case MapPbjectType.Pokemon:
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    CreatePokemonMarker(newMapObj);
                                }
                                break;
                            case MapPbjectType.SetLured:
                                if (_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        var shape = Properties.Resources.pstop_lured.ToImage("Lured PokeStop");
                                        shape.ContextMenu = FindResource("PokestopContextMenu") as ContextMenu;
                                        _bot.MapMarkers[newMapObj.Uid].Shape = shape;
                                    }));
                                }
                                break;
                            case MapPbjectType.SetUnLured:
                                if (_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        var shape = Properties.Resources.pstop.ToImage("PokeStop");
                                        shape.ContextMenu = FindResource("PokestopContextMenu") as ContextMenu;
                                        _bot.MapMarkers[newMapObj.Uid].Shape = shape;
                                    }));
                                }
                                break;
                            case MapPbjectType.Gym:
                                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        CreateGymMarker(newMapObj);
                                    }));
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("[PS_CREATE_FAIL]" + ex.Message);
                        // ignored
                    }
                }
                await Task.Delay(10);
            }
        }

        private async Task CreatePokestopMarker(string name, NewMapObject newMapObj)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                var shape = newMapObj.OType == MapPbjectType.PokestopLured
                    ? Properties.Resources.pstop_lured.ToImage("Lured - " + name)
                    : Properties.Resources.pstop.ToImage(name);

                shape.ContextMenu = FindResource("PokestopContextMenu") as ContextMenu;
                var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                {
                    Shape = shape,
                    Offset = new Point(-16, -32),
                    ZIndex = 5,
                    Tag = _bot
                };
                AddMarker(marker);
                if (!_bot.MapMarkers.ContainsKey(newMapObj.Uid))
                    _bot.MapMarkers.Add(newMapObj.Uid, marker);
            }));
        }

        private void CreateGymMarker(NewMapObject newMapObj)
        {
            try
            {
                var team = (TeamColor?) newMapObj.ExtraData[0];
                Image shape;
                switch (team)
                {
                    case TeamColor.Neutral:
                        shape = Properties.Resources.gym.ToImage(newMapObj.OName);
                        break;
                    case TeamColor.Blue:
                        shape = Properties.Resources.gym_blue.ToImage(newMapObj.OName);
                        break;
                    case TeamColor.Red:
                        shape = Properties.Resources.gym_red.ToImage(newMapObj.OName);
                        break;
                    case TeamColor.Yellow:
                        shape = Properties.Resources.gym_yellow.ToImage(newMapObj.OName);
                        break;
                    case null:
                        shape = Properties.Resources.gym.ToImage(newMapObj.OName);
                        break;
                    default:
                        shape = Properties.Resources.gym.ToImage(newMapObj.OName);
                        break;
                }

                var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                {
                    Shape = shape,
                    Offset = new Point(-16, -32),
                    ZIndex = 6
                };
                AddMarker(marker);
                _bot.MapMarkers.Add(newMapObj.Uid, marker);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        #endregion

        private void RemoveMarker(string uid, GMapMarker marker)
        {
            pokeMap.Markers.Remove(marker);
            MainWindow.BotWindow.GlobalMapView.RemoveMarker(marker);
            _bot.MapMarkers.Remove(uid);
        }

        private void CreatePokemonMarker(NewMapObject newMapObj)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                var pokemon = (PokemonId) Enum.Parse(typeof(PokemonId), newMapObj.OName);

                var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                {
                    Shape = pokemon.ToImage(),
                    Offset = new Point(-12, -30),
                    ZIndex = 10,
                    Tag = "pm"
                };
                AddMarker(marker);
                _bot.MapMarkers.Add(newMapObj.Uid, marker);
            }));
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
            SetSpeedFactor(sl.Value);
        }

        private void SetSpeedFactor(double factor)
        {
            l_moveSpeedFactor.Text = factor.ToString("N5");
            if (_bot == null || _loadingUi) return;
            _bot.GlobalSettings.LocationSettings.MoveSpeedFactor = factor;
        }

        private void cb_keepPokemonMarkers_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box?.IsChecked != null)
            {
                if ((bool) box.IsChecked)
                {
                    _globalSettings.KeepPokeMarkersOnMap = true;
                }
                else
                {
                    _globalSettings.KeepPokeMarkersOnMap = false;
                    foreach (var bot in MainWindow.BotsCollection)
                        foreach (var item in bot.MarkersDelayRemove)
                            bot.MarkersQueue.Enqueue(item);
                }
            }
        }

        private void mapFollowThePlayer_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box?.IsChecked != null) _globalSettings.FollowTheWhiteRabbit = (bool) box.IsChecked;
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

        private void MapProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            var provider = cb?.SelectedItem as MapProvider?;
            if (provider == null) return;

            _globalSettings.ProviderEnum = (MapProvider) provider;
            _globalSettings.LoadProperProvider();
        }

        private void sl_moveSpeedFactor_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var sl = (sender as Slider);
            if (sl == null) return;
            sl.Value = 1;
            SetSpeedFactor(1);
        }

        private void MapSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (e.Key != Key.Enter) return;
            var searchText = tb.Text;
            if (!string.IsNullOrEmpty(searchText))
            {
                pokeMap.SetPositionByKeywords(searchText);
            }
        }

        private void MapSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text == @"Search for location")
                tb.Text = "";
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
