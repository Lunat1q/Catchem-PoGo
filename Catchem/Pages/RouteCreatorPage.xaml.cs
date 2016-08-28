using System;
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
using GMap.NET;

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


    }
}
