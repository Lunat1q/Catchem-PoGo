using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Catchem.Classes;
using Catchem.Extensions;
using Catchem.Helpers;
using Catchem.UiTranslation;
using GeoCoordinatePortable;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.DataStorage;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for RouteCreatorPage.xaml
    /// </summary>
    public partial class RouteCreatorPage
    {
        private CatchemSettings _globalSettings;

        private readonly ObservableCollection<RouteMarker> _mapPoints = new ObservableCollection<RouteMarker>();
        private GMapRoute _currentRoute;
        private bool _builded;
        private List<GeoCoordinate> _buildedRoute;
        private bool _prefferMapzen;
        private bool _manualRoute;
        private bool _showExactPoke;
        private int _currentIndex = 1;
        private readonly List<PokemonSeen> _mappedPokemons = new List<PokemonSeen>();
        private readonly Dictionary<string, GMapMarker> _pokestops = new Dictionary<string, GMapMarker>();
        private readonly Dictionary<int, GMapMarker> _pokemons = new Dictionary<int, GMapMarker>();
        private Task _routeBuilder;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationToken Token => _cts.Token;

        private CancellationTokenSource _ctsPrev = new CancellationTokenSource();
        private Task _showPsTask;
        private Task _showHeatMapTask;
        private CancellationToken PrevToken => _ctsPrev.Token;

        public bool StartExist
        {
            get { return _mapPoints.Any(x => x.IsStart); }
        }

        public void SetGlobalSettings(CatchemSettings settings)
        {
            _globalSettings = settings;
            RouteCreatorMap.MapProvider = _globalSettings.Provider;
            _globalSettings.BindNewMapProvider((provider) =>
            {
                RouteCreatorMap.MapProvider = provider;
                return true;
            });
            RoutesListBox.ItemsSource = _globalSettings.Routes;
            var bot = MainWindow.BotsCollection.FirstOrDefault();
            if (bot != null)
            {
                SetMapPostion(bot.GlobalSettings.LocationSettings.DefaultLatitude,
                    bot.GlobalSettings.LocationSettings.DefaultLongitude);
            }
        }

        public void SetMapPostion(double lat, double lng)
        {
            RouteCreatorMap.Position = new PointLatLng(lat, lng);
        }

        public RouteCreatorPage()
        {
            InitializeComponent();
            InitializeMap();
        }

        private void InitializeMap()
        {
            RouteCreatorMap.Bearing = 0;
            RouteCreatorMap.CanDragMap = true;
            RouteCreatorMap.DragButton = MouseButton.Left;
            RouteCreatorMap.MaxZoom = 18;
            RouteCreatorMap.MinZoom = 2;
            RouteCreatorMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            RouteCreatorMap.IgnoreMarkerOnMouseWheel = true;
            RouteCreatorMap.ShowCenter = false;
            RouteCreatorMap.ShowTileGridLines = false;
            RouteCreatorMap.Zoom = 18;
            GMap.NET.MapProviders.GMapProvider.WebProxy = System.Net.WebRequest.GetSystemWebProxy();
            GMap.NET.MapProviders.GMapProvider.WebProxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            RouteCreatorMap.MapProvider = GMap.NET.MapProviders.GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            RouteCreatorMap.Position = new PointLatLng(-37.803674, 144.958717);
        }

        private void RouteCreatorMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SlMapZoom.Value = RouteCreatorMap.Zoom;
        }

        private void sl_mapZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = sender as Slider;
            if (sl == null) return;
            RouteCreatorMap.Zoom = (int) sl.Value;
        }

        internal void AddMarker(RouteMarker marker, bool starter)
        {
            if (starter)
            {
                try
                {
                    while (_mapPoints.Any(x => x.IsStart))
                    {
                        var mark = _mapPoints.First(x => x.IsStart);
                        RemoveMarker(mark);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            _mapPoints.Add(marker);
            RouteCreatorMap.Markers.Add(marker.Marker);
            UpdateMarkerCounter();
            CheckRouteServicePrefer();
        }

        private void UpdateMarkerCounter()
        {
            PointsNumber.Text =
                TranslationEngine.GetDynamicTranslationString("%NUMBER_OF_WAYPOINTS%", "Number of waypoints:") +
                $" {_mapPoints.Count}";
        }

        private void RemoveMarker(RouteMarker rm)
        {
            RouteCreatorMap.Markers.Remove(rm.Marker);
            rm.Marker.Clear();
            _mapPoints.Remove(rm);
            UpdateMarkerCounter();
            CheckRouteServicePrefer();
        }

        private void MiSetStart_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var cm = mi?.Parent as ContextMenu;
            var point = cm?.Tag as Point?;
            if (point == null) return;
            CreateNewMarker((Point) point, true);
        }

        private void MiSetWp_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var cm = mi?.Parent as ContextMenu;
            var point = cm?.Tag as Point?;
            if (point == null) return;
            CreateNewMarker((Point) point, false);
        }

        private void CreateNewMarker(Point point, bool starter)
        {
            _builded = false;
            var rPoint = point;
            var mapPos = RouteCreatorMap.FromLocalToLatLng((int) rPoint.X, (int) rPoint.Y);
            CreateNewMarker(mapPos, starter);

            RunDataTasks(mapPos);
        }

        internal void RunDataTasks(PointLatLng mapPos, double dist = 500)
        {
            if ((_showPsTask != null &&
                 (_showPsTask.Status == TaskStatus.Running || _showPsTask.Status == TaskStatus.WaitingForActivation)) ||
                (_showHeatMapTask != null &&
                 (_showHeatMapTask.Status == TaskStatus.Running ||
                  _showHeatMapTask.Status == TaskStatus.WaitingForActivation)))
            {
                return;
            }

            try
            {
                _ctsPrev.Cancel();
                _ctsPrev = new CancellationTokenSource();
                _showPsTask = Task.Run(() => ShowPokestops(mapPos.Lat, mapPos.Lng, dist, PrevToken), PrevToken);
                _showHeatMapTask = Task.Run(() => ShowPokemons(mapPos.Lat, mapPos.Lng, dist, PrevToken), PrevToken);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async Task ShowPokestops(double lat, double lng, double dist, CancellationToken token)
        {
            var pokeStops = DbHandler.GetPokestopsForCoords(lat, lng, dist);
            foreach (var ps in pokeStops)
            {
                if (_pokestops.ContainsKey(ps.Id)) continue;
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    var markerShape =
                        Properties.Resources.pstop.ToImage(string.IsNullOrEmpty(ps.Name)
                            ? TranslationEngine.GetDynamicTranslationString("%POKESTOP%", "PokeStop")
                            : ps.Name);
                    var marker = new GMapMarker(new PointLatLng(ps.Latitude, ps.Longitude))
                    {
                        Shape = markerShape,
                        Offset = new Point(-16, -32),
                        ZIndex = 5
                    };
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        if (!_pokestops.ContainsKey(ps.Id))
                            _pokestops.Add(ps.Id, marker);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    RouteCreatorMap.Markers.Add(marker);
                }));
            }
        }

        private void MapSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (e.Key != Key.Enter) return;
            var searchText = tb.Text;
            if (!string.IsNullOrEmpty(searchText))
            {
                RouteCreatorMap.SetPositionByKeywords(searchText);
            }
        }

        private void MapSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text == @"Search for location")
                tb.Text = "";
        }

        private async void DrawPokemonsHeatMap(CancellationToken token)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                foreach (var p in _pokemons.Values)
                    RouteCreatorMap.Markers.Remove(p);
            }));

            _pokemons.Clear();

            var pokeList = _mappedPokemons.ToList();

            var heatMapMarkers =
                await HeatMapHelper.GuildPokemonSeenHeatMap(pokeList, _globalSettings.HeatMapClusterRadius, Dispatcher, token);
            if (heatMapMarkers == null) return;
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                foreach (var marker in heatMapMarkers)
                {
                    if (_pokemons.ContainsKey(marker.Key)) continue;

                    _pokemons.Add(marker.Key, marker.Value);
                    RouteCreatorMap.Markers.Add(marker.Value);
                }
            }));
        }

        private void DrawExactPoke(IEnumerable<PokemonSeen> poke, CancellationToken token)
        {
            try
            {
                foreach (var p in poke)
                {
                    if (_pokemons.ContainsKey(p.Id)) continue;
                    var markerShape = p.PokemonId.ToImage(p.SeenTime);
                    token.ThrowIfCancellationRequested();
                    var marker = new GMapMarker(new PointLatLng(p.Latitude, p.Longitude))
                    {
                        Shape = markerShape,
                        Offset = new Point(-16, -32),
                        ZIndex = 2,
                    };
                    RouteCreatorMap.Markers.Add(marker);
                    _pokemons.Add(p.Id, marker);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }



        private async Task ShowPokemons(double lat, double lng, double dist, CancellationToken token)
        {
            var poke = DbHandler.GetPokemonSeenForCoords(lat, lng, dist);
            //poke = poke.OrderBy(x => x.PokemonId.HowRare()).Take(50);
            var pokemonSeens = poke as IList<PokemonSeen> ?? poke.ToList();
            if (poke == null || !pokemonSeens.Any()) return;
            var pokeToShow = pokemonSeens.Where(x => _mappedPokemons.All(v => v.Id != x.Id)).ToList();
            _mappedPokemons.AddRange(pokeToShow);

            if (_showExactPoke)
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    DrawExactPoke(pokeToShow, token);
                }));
            }
            else
            {
                DrawPokemonsHeatMap(token);
            }
        }

        private void CreateNewMarker(PointLatLng mapPos, bool starter)
        {
            var marker = new GMapMarker(mapPos)
            {
                Offset = starter ? new Point(-24, -48) : new Point(-16, -32),
                ZIndex = int.MaxValue,
                Position = mapPos
            };

            var imgSource = starter
                ? Properties.Resources.force_move.LoadBitmap()
                : Properties.Resources.wp.LoadBitmap();

            var tooltipText = starter
                ? TranslationEngine.GetDynamicTranslationString("%ROUTE_MARKER_START%", "Route Marker - START")
                : TranslationEngine.GetDynamicTranslationString("%ROUTE_MARKER_WP%", "Route Marker - Waypoint") +
                  $" {_currentIndex++}";

            var rm = new RouteMarker(imgSource, tooltipText, RouteCreatorMap, this)
            {
                IsStart = starter,
                Location = new GeoCoordinate(mapPos.Lat, mapPos.Lng),
                Marker = marker
            };
            marker.Shape = rm;
            rm.MouseLeftButtonDown += delegate
            {
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    RemoveMarker(rm);
                }
            };

            AddMarker(rm, starter);
        }



        internal class RouteMarker : Image
        {
            public GMapMarker Marker;
            public GeoCoordinate Location;
            public bool IsStart;
            private readonly GMapControl _map;
            private readonly RouteCreatorPage _page;

            public RouteMarker(ImageSource imgsource, string tooltipText, GMapControl map, RouteCreatorPage page)
            {
                Source = imgsource;
                var tt = new ToolTip { Content = tooltipText };
                ToolTip = tt;
                _map = map;
                _page = page;
                MouseMove += Marker_MouseMove;
                MouseLeftButtonUp += Marker_MouseLeftButtonUp;
                MouseLeftButtonDown += Marker_MouseLeftButtonDown;
            }

            private void Marker_MouseMove(object sender, MouseEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed || !IsMouseCaptured) return;

                var p = e.GetPosition(_map);
                Marker.Position = _map.FromLocalToLatLng((int)p.X, (int)p.Y);
                Location = new GeoCoordinate(Marker.Position.Lat, Marker.Position.Lng);
            }

            private void Marker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (!IsMouseCaptured)
                {
                    Mouse.Capture(this);
                }
            }

            private void Marker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                if (!IsMouseCaptured) return;
                Mouse.Capture(null);
                var p = e.GetPosition(_map);
                var pos = _map.FromLocalToLatLng((int)p.X, (int)p.Y);
                _page.Dispatcher.BeginInvoke(
                    new ThreadStart(
                        delegate { _page.RunDataTasks(pos); }));
            }
        }

        private void RouteCreatorMap_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Get cursor position in screen coordinates
            var screenPoint = Mouse.GetPosition(RouteCreatorMap);

            RouteCm.Tag = screenPoint;
        }

        private string GetWorkingRouting(out BotWindowData bot)
        {
            var botGoogle =
                MainWindow.BotsCollection.FirstOrDefault(
                    x => !string.IsNullOrEmpty(x.GlobalSettings.LocationSettings.GoogleDirectionsApiKey));

            var botMapzen =
                MainWindow.BotsCollection.FirstOrDefault(
                    x => !string.IsNullOrEmpty(x.GlobalSettings.LocationSettings.MapzenApiKey));

            bot = _prefferMapzen
                ? (botMapzen ?? botGoogle)
                : (botGoogle ?? botMapzen);

            if (botGoogle == null && botMapzen == null) return "error";

            return _prefferMapzen
                ? (botMapzen != null ? "mapzen" : "google")
                : (botGoogle != null ? "google" : "mapzen");
        }

        private void CheckRouteServicePrefer()
        {
            if (_mapPoints.Count > 20 && !_manualRoute)
            {
                _prefferMapzen = true;
                PreferMapzenOverGoogleCb.IsChecked = true;
                PreferMapzenOverGoogleCb.IsEnabled = false;
            }
            else if (_mapPoints.Count <= 20 && (_manualRoute || !PreferMapzenOverGoogleCb.IsEnabled))
            {
                PreferMapzenOverGoogleCb.IsEnabled = true;
            }
        }

        private void UpdateProgress(string text, int val)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                ProgressText.Text = text;
                BuildingProgressBar.Value = val;
            }));
        }


        private async Task BuildTheRouteTask(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (_mapPoints.Count < 2) return;
                await Dispatcher.BeginInvoke(new ThreadStart(CheckRouteServicePrefer));
                if (_mapPoints.Count > 47 && !_manualRoute)
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        MessageBox.Show(
                            TranslationEngine.GetDynamicTranslationString("%TOO_MANY_ROUTE_POINTS%",
                                "Too many waypoints, try to reduce them to 47, or wait for next releases, where that limit will be increased!"),
                            "Routing Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }));
                    return;
                }
                UpdateProgress(TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_2%", "Started!..."), 0);
                BotWindowData bot;
                var route = GetWorkingRouting(out bot);
                if (route == "error" && !_manualRoute)
                {
                    MessageBox.Show(
                        TranslationEngine.GetDynamicTranslationString("%NO_ROUTE_API_FOUND%",
                            "You have to enter Google Direction API or Mapzen Valhalla API to any of your bots, before creating a route"),
                        "API Key Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var start = _mapPoints.FirstOrDefault(x => x.IsStart) ?? _mapPoints.First();
                UpdateProgress(TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_3%", "Started!..."), 10);
                RoutingResponse response = null;
                var cycleWp = _mapPoints.Where(x => !x.IsStart).Select(x => x.Location).ToList();
                cycleWp.Add(start.Location);
                List<GeoCoordinate> routePoints;
                token.ThrowIfCancellationRequested();
                if (!_manualRoute)
                {
                    if (route == "google")
                    {
                        response = GoogleRouting.GetRoute(start.Location, null, bot.Session, cycleWp, true);
                    }
                    else if (route == "mapzen")
                    {
                        response = MapzenRouting.GetRoute(start.Location, null, bot.Session, cycleWp, true);
                    }
                    if (response?.Coordinates == null || response.Coordinates.Count == 0) return;
                    routePoints = response.Coordinates.Select(wp => new GeoCoordinate(wp[0], wp[1])).ToList();
                }
                else
                {
                    cycleWp.Insert(0, start.Location);
                    routePoints = new List<GeoCoordinate>(cycleWp);
                }
                token.ThrowIfCancellationRequested();
                UpdateProgress(
                    TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_4%", "Handling result..."), 60);
                _currentRoute?.Points?.Clear();
                if (_currentRoute == null)
                    _currentRoute = new GMapRoute(new List<PointLatLng>());

                await RouteCreatorMap.Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    RouteCreatorMap.Markers.Add(_currentRoute);
                }));

                token.ThrowIfCancellationRequested();
                UpdateProgress(
                    TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_5%", "Requesting altitude..."), 70);
                _buildedRoute = new List<GeoCoordinate>(routePoints);
                token.ThrowIfCancellationRequested();
                foreach (var item in routePoints)
                {
                    _currentRoute.Points?.Add(new PointLatLng(item.Latitude, item.Longitude));
                }
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    try
                    {
                        _currentRoute.RegenerateShape(RouteCreatorMap);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }));
                var path = _currentRoute?.Shape as Path;
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (path != null)
                        path.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }));


                bot = MainWindow.BotsCollection.FirstOrDefault(
                    x => !string.IsNullOrEmpty(x.GlobalSettings.LocationSettings.MapzenApiKey));
                if (bot != null)
                {
                    await bot.Session.MapzenApi.FillAltitude(_buildedRoute.ToList(), token: token);
                }
                UpdateProgress(TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_6%", "Done!"), 100);
                _builded = true;
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
        }

        private void BuildTheRoute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_routeBuilder != null && _routeBuilder.Status == TaskStatus.Running) return;
                _cts = new CancellationTokenSource();
                _routeBuilder = Task.Run(() => BuildTheRouteTask(Token), Token);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void ClearTheRoute_Click(object sender, RoutedEventArgs e)
        {
            ClearRouteBuilder();
        }

        private void ClearRouteBuilder()
        {
            _currentIndex = 1;
            _currentRoute?.Points.Clear();
            _currentRoute?.RegenerateShape(RouteCreatorMap);
            BuildingProgressBar.Value = 0;
            ProgressText.Text = TranslationEngine.GetDynamicTranslationString("%ROUTE_PROGRESS_1%", "Route progress");
            _builded = false;
            while (_mapPoints.Any())
            {
                RemoveMarker(_mapPoints[0]);
            }
            RouteCreatorMap.Markers.Clear();
            _pokestops.Clear();
            _pokemons.Clear();
            _mappedPokemons.Clear();
            if (_routeBuilder != null && _routeBuilder.Status == TaskStatus.Running)
            {
                _cts.Cancel();
            }
            _ctsPrev.Cancel();
            _ctsPrev = new CancellationTokenSource();
            _showPsTask = null;
            _showHeatMapTask = null;
        }

        private void SaveTheRoute_Click(object sender, RoutedEventArgs e)
        {
            var routeName = NewRouteNameBox.Text;
            if (string.IsNullOrEmpty(routeName)) return;
            if (!_builded)
            {
                MessageBox.Show(
                    TranslationEngine.GetDynamicTranslationString("%ERR_BUILD_THE_ROUTE%", "Build the route first"),
                    "Routing error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (
                _globalSettings.Routes.Any(
                    x => string.Equals(x.Name, routeName, StringComparison.CurrentCultureIgnoreCase)))
                routeName += "_" + DeviceSettings.RandomString(5);
            var route = new BotRoute
            {
                Name = routeName,
                InitialWp = _mapPoints.Select(x => x.Location).ToList(),
                Route = new CustomRoute()
                {
                    RoutePoints = new List<GeoCoordinate>(_buildedRoute)
                }
            };
            _globalSettings.Routes.Add(route);
            _globalSettings.Save();
            NewRouteNameBox.Text = string.Empty;
        }

        private void RoutesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = sender as ListBox;
            if (lb == null || lb.SelectedIndex < 0) return;
            var selectedRoute = lb.SelectedItem as BotRoute;
            if (selectedRoute == null) return;
            ClearRouteBuilder();
            var start = true;
            foreach (var wp in selectedRoute.InitialWp)
            {
                CreateNewMarker(new PointLatLng(wp.Latitude, wp.Longitude), start);
                if (start) start = false;
            }
            double dist;
            var mid =
                HeatMapHelper.GetMidPointAndRadius(
                    selectedRoute.InitialWp.Select(x => new PointLatLng(x.Latitude, x.Longitude)), out dist);
            RunDataTasks(mid, dist);
            RouteCreatorMap.ZoomAndCenterMarkers(null);
        }

        private void PreferMapzenOverGoogleCb_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb?.IsChecked == null) return;
            _prefferMapzen = (bool) cb.IsChecked;
        }

        private void ManualRoute_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb?.IsChecked == null) return;
            _manualRoute = (bool) cb.IsChecked;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb?.IsChecked == null) return;

            foreach (var p in _pokemons)
            {
                RouteCreatorMap.Markers.Remove(p.Value);
            }
            _pokemons.Clear();
            _showExactPoke = (bool) cb.IsChecked;
            if (_showExactPoke)
            {
                DrawExactPoke(_mappedPokemons, PrevToken);
            }
            else
            {
                DrawPokemonsHeatMap(PrevToken);
            }
        }
       
        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) && RouteCreatorMap != null)
            {
                RouteCreatorMap.CanDragMap = false;
            }
        }

        private void RouteMapCreator_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.KeyDown += HandleKeyPress;
                window.KeyUp += WindowOnKeyUp;
            }
        }

        private void WindowOnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if ((keyEventArgs.Key == Key.LeftCtrl || keyEventArgs.Key == Key.RightCtrl) && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && RouteCreatorMap != null)
            {
                RouteCreatorMap.CanDragMap = true;
            }
        }

        private void ImportRoute_Click(object sender, RoutedEventArgs e)
        {
            ImportExportRoute.Visibility = Visibility.Visible;
            ImportExportHeading.Text = TranslationEngine.GetDynamicTranslationString("%IMPORT_ROUTE%", "Import Route");
            ImportExportRouteSubmit.Visibility = Visibility.Visible;
            ImportExportInstructions.Text = TranslationEngine.GetDynamicTranslationString("%IMPORT_INSTRUCTION%", "Enter a route below to Import it into Catchem, once entered click import.");
            ImportExportRouteText.Clear();
        }

        private void ExportRoute_Click(object sender, RoutedEventArgs e)
        {
            ImportExportRouteText.Clear();
            var points = _mapPoints.Select(x=>x.Location).ToList();
            string encodedRoute = null;

            var firstOrDefault = points.FirstOrDefault();
            if (firstOrDefault == null) return;
            try
            {
                encodedRoute = RoutingUtils.EncodeToPolyline(points);
            }
            catch (Exception)
            {
                //do nothing
            }
            if (encodedRoute == null) return;
            ImportExportRouteText.Text = encodedRoute;
            ImportExportRoute.Visibility = Visibility.Visible;
            ImportExportHeading.Text = TranslationEngine.GetDynamicTranslationString("%EXPORT_ROUTE%","Export Route");
            ImportExportRouteSubmit.Visibility = Visibility.Collapsed;
            ImportExportInstructions.Text = TranslationEngine.GetDynamicTranslationString("%EXPORT_INSTRUCTION%", "Copy the below route to create a backup or share with others.");
        }

        private void ImportRouteSubmit_Click(object sender, RoutedEventArgs e)
        {
            ImportExportRoute.Visibility = Visibility.Collapsed;
            var encRoute = ImportExportRouteText.Text;
            if (encRoute == null) return;
            List<GoogleLocation> decodedPoints;
            try
            {
                decodedPoints = RoutingUtils.DecodePolyline(encRoute).ToList();
            }
            catch (Exception)
            {
                return;
            }
            ClearRouteBuilder();
            var i = 0;
            foreach (var point in decodedPoints)
            {

                var pointlatlon = new PointLatLng(point.lat,point.lng);
                CreateImportedMarker(pointlatlon, i == 0);
                i++;
            }
            if (i > 0)
            {
                try
                {
                    if (_routeBuilder != null && _routeBuilder.Status == TaskStatus.Running) return;
                    _cts = new CancellationTokenSource();
                    _routeBuilder = Task.Run(() => BuildTheRouteTask(Token), Token);
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            ImportExportRouteText.Clear();
            var startMarker = decodedPoints.FirstOrDefault();
            if (startMarker != null) RouteCreatorMap.Position = new PointLatLng(startMarker.lat, startMarker.lng);
            RouteCreatorMap.ZoomAndCenterMarkers(null);
        }

        private void ImportExportRouteCancel_Click(object sender, RoutedEventArgs e)
        {
            ImportExportRoute.Visibility = Visibility.Collapsed;
            ImportExportRouteText.Clear();
        }

        private void CreateImportedMarker(PointLatLng point, bool starter)
        {
            _builded = false;
            CreateNewMarker(point, starter);
            RunDataTasks(point);
        }

    }
}