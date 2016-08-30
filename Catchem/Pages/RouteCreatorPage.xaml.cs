using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Catchem.Classes;
using Catchem.Extensions;
using GeoCoordinatePortable;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.PoGoUtils;

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
        private int _currentIndex = 1;

        public bool StartExist
        {
            get { return _mapPoints.Any(x => x.IsStart); }
        }

        public void SetGlobalSettings(CatchemSettings settings)
        {
            _globalSettings = settings;
            RouteCreatorMap.MapProvider = _globalSettings.Provider;
            _globalSettings.BindNewMapProbider((provider) =>
            {
                RouteCreatorMap.MapProvider = provider;
                return true;
            });
            RoutesListBox.ItemsSource = _globalSettings.Routes;
            var bot = MainWindow.BotsCollection.FirstOrDefault();
            if (bot != null)
            {
                RouteCreatorMap.Position = new PointLatLng(bot.GlobalSettings.LocationSettings.DefaultLatitude,
                    bot.GlobalSettings.LocationSettings.DefaultLongitude);
            }
        }

        public RouteCreatorPage()
        {
            InitializeComponent();
            InitializeMap();
        }

        private async void InitializeMap()
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
            await Task.Delay(10);
        }

        private void RouteCreatorMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SlMapZoom.Value = RouteCreatorMap.Zoom;
        }
        private void sl_mapZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null) return;
            RouteCreatorMap.Zoom = (int)sl.Value;
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
            CheckRouteServicePreffer();
        }

        private void UpdateMarkerCounter()
        {
            PointsNumber.Content = $"Number of waypoints: {_mapPoints.Count}";
        }

        private void RemoveMarker(RouteMarker rm)
        {
            RouteCreatorMap.Markers.Remove(rm.Marker);
            rm.Marker.Clear();
            _mapPoints.Remove(rm);
            UpdateMarkerCounter();
            CheckRouteServicePreffer();
        }

        private void MiSetStart_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var cm = mi?.Parent as ContextMenu;
            var point = cm?.Tag as Point?;
            if (point == null) return;
            CreateNewMarker((Point)point, true);
        }

        private void MiSetWp_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var cm = mi?.Parent as ContextMenu;
            var point = cm?.Tag as Point?;
            if (point == null) return;
            CreateNewMarker((Point)point, false);
        }

        private void CreateNewMarker(Point point, bool starter)
        {
            _builded = false;
            var rPoint = point;
            var mapPos = RouteCreatorMap.FromLocalToLatLng((int) rPoint.X, (int) rPoint.Y);
            CreateNewMarker(mapPos, starter);
        }

        private void CreateNewMarker(PointLatLng mapPos, bool starter)
        {
            var markerShape = starter ? Properties.Resources.force_move.ToImage("Route Marker - START") :
               Properties.Resources.wp.ToImage($"Route Marker - Waypoint {_currentIndex++}");
            var marker = new GMapMarker(mapPos)
            {
                Shape = markerShape,
                Offset = starter ? new Point(-24, -48) : new Point(-16, -32),
                ZIndex = int.MaxValue,
                Position = mapPos
            };
            var rm = new RouteMarker
            {
                IsStart = starter,
                Location = new GeoCoordinate(mapPos.Lat, mapPos.Lng),
                Marker = marker
            };
            markerShape.MouseLeftButtonDown += delegate { RemoveMarker(rm); };
            AddMarker(rm, starter);
        }



        internal class RouteMarker
        {
            public GMapMarker Marker;
            public GeoCoordinate Location;
            public bool IsStart;
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
                    x => !string.IsNullOrEmpty(x.GlobalSettings.LocationSettings.MapzenValhallaApiKey));

            bot = _prefferMapzen
                ? (botMapzen ?? botGoogle)
                : (botGoogle ?? botMapzen);

            if (botGoogle == null && botMapzen == null) return "error";

            return _prefferMapzen
                ? (botMapzen != null ? "mapzen" : "google")
                : (botGoogle != null ? "google" : "mapzen");
        }

        private void CheckRouteServicePreffer()
        {
            if (_mapPoints.Count > 20 && !_manualRoute)
            {
                _prefferMapzen = true;
                PrefferMapzenOverGoogleCb.IsChecked = true;
                PrefferMapzenOverGoogleCb.IsEnabled = false;
            }
            else if(_mapPoints.Count <= 20 && (_manualRoute || !PrefferMapzenOverGoogleCb.IsEnabled))
            {
                PrefferMapzenOverGoogleCb.IsEnabled = true;
            }
        }

        private async void BuildTheRoute_Click(object sender, RoutedEventArgs e)
        {
            if (_mapPoints.Count < 2) return;
            CheckRouteServicePreffer();
            if (_mapPoints.Count > 47 && !_manualRoute)
            {
                MessageBox.Show(
                    "Too many waypoints, try to reduce them to 47, or wait for next releases, where that limit will be increased!",
                    "Routing Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            BuildingProgressBar.Value = 0;
            BotWindowData bot;
            var route = GetWorkingRouting(out bot);
            if (route == "error" && !_manualRoute)
            {
                MessageBox.Show(
                    "You have to enter Google Direction API or Mapzen Valhalla API to any of your bots, before creating a route",
                    "API Key Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var start = _mapPoints.FirstOrDefault(x => x.IsStart) ?? _mapPoints.First();
            BuildingProgressBar.Value = 10;

            RoutingResponse response = null;
            var cycleWp = _mapPoints.Where(x => !x.IsStart).Select(x => x.Location).ToList();
            cycleWp.Add(start.Location);
            List<GeoCoordinate> routePoints;

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
            BuildingProgressBar.Value = 60;
            _currentRoute?.Points?.Clear();
            if (_currentRoute == null)
            {
                _currentRoute = new GMapRoute(new List<PointLatLng>());
                RouteCreatorMap.Markers.Add(_currentRoute);
            }
            BuildingProgressBar.Value = 70;
            _buildedRoute = new List<GeoCoordinate>(routePoints);

            foreach (var item in routePoints)
            {
                _currentRoute.Points?.Add(new PointLatLng(item.Latitude, item.Longitude));
            }
            
            _currentRoute?.RegenerateShape(RouteCreatorMap);
            var path = _currentRoute?.Shape as Path;
            if (path != null)
                path.Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 0));


            bot = MainWindow.BotsCollection.FirstOrDefault(
                       x => !string.IsNullOrEmpty(x.GlobalSettings.LocationSettings.MapzenApiElevationKey));
            if (bot != null)
            {
               await bot.Session.MapzenApi.FillAltitude(_buildedRoute.ToList());
            }
            BuildingProgressBar.Value = 100;
            _builded = true;
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
            _builded = false;
            while (_mapPoints.Any())
            {
                RemoveMarker(_mapPoints[0]);
            }
        }

        private void SaveTheRoute_Click(object sender, RoutedEventArgs e)
        {
            var routeName = NewRouteNameBox.Text;
            if (string.IsNullOrEmpty(routeName)) return;
            if (!_builded)
            {
                MessageBox.Show(
                    "Build the route first",
                    "Routing error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_globalSettings.Routes.Any(x => string.Equals(x.Name, routeName, StringComparison.CurrentCultureIgnoreCase)))
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
            RouteCreatorMap.ZoomAndCenterMarkers(null);
        }

        private void PrefferMapzenOverGoogleCb_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb?.IsChecked == null) return;
            _prefferMapzen = (bool)cb.IsChecked;
        }

        private void ManualRoute_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb?.IsChecked == null) return;
            _manualRoute = (bool)cb.IsChecked;
        }
    }
}