using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Catchem.Classes;
using Catchem.Extensions;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for RouteCreatorPage.xaml
    /// </summary>

       

        public partial class RouteCreatorPage : UserControl
    {
       

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

        private void RouteCreatorMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var mousePos = e.GetPosition(RouteCreatorMap);
                //Getting real coordinates from mouse click
                var mapPos = RouteCreatorMap.FromLocalToLatLng((int) mousePos.X, (int) mousePos.Y);
                var lat = mapPos.Lat;
                var lng = mapPos.Lng;
                // _routeMarkers.Enqueue(new PointLatLng(lat, lng));
                var marker = new GMapMarker(mapPos)
                {
                    Shape = Properties.Resources.force_move.ToImage("Route Marker"),
                    Offset = new Point(-24, -48),
                    ZIndex = int.MaxValue
                };
                AddMarker(marker);
            }
            if (e.ChangedButton == MouseButton.Right)
            {
                var mousePos = e.GetPosition(RouteCreatorMap);
                //Getting real coordinates from mouse click
                var mapPos = RouteCreatorMap.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y);
                var minLat = mapPos.Lat - 0.0001;
                var maxLat = mapPos.Lat + 0.0001;
                var minLng = mapPos.Lng - 0.0001;
                var maxLng = mapPos.Lng + 0.0001;
                foreach (var marker in RouteCreatorMap.Markers)
                {
                    if (marker.Position.Lat >= minLat && marker.Position.Lat <= maxLat && marker.Position.Lng >= minLng && marker.Position.Lng <= maxLng)
                    {
                        RouteCreatorMap.Markers.Remove(new GMapMarker(marker.Position));
                        marker.Clear();
                    }
                
                }
            }
        }


        private void RouteCreatorMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            sl_mapZoom.Value = RouteCreatorMap.Zoom;
        }
        private void sl_mapZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null) return;
            RouteCreatorMap.Zoom = (int)sl.Value;
        }

        public void AddMarker(GMapMarker marker)
        {
            RouteCreatorMap.Markers.Add(marker);
        }


       
    }
}
