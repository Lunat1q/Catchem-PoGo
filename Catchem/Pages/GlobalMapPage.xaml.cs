using GMap.NET;
using GMap.NET.WindowsPresentation;
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

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for GlobalMapPage.xaml
    /// </summary>
    public partial class GlobalMapPage : UserControl
    {
        private CatchemSettings _globalSettings;
        
        public GlobalMapPage()
        {
            InitializeComponent();
            pokeMap.Bearing = 0;
            pokeMap.CanDragMap = true;
            pokeMap.DragButton = MouseButton.Left;
            pokeMap.MaxZoom = 18;
            pokeMap.MinZoom = 2;
            pokeMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            pokeMap.IgnoreMarkerOnMouseWheel = true;
            pokeMap.ShowCenter = false;
            pokeMap.ShowTileGridLines = false;
            pokeMap.Zoom = 2;
            GMap.NET.MapProviders.GMapProvider.WebProxy = System.Net.WebRequest.GetSystemWebProxy();
            GMap.NET.MapProviders.GMapProvider.WebProxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            pokeMap.MapProvider = GMap.NET.MapProviders.GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            
        }

        public void SetGlobalSettings(CatchemSettings settings)
        {
            _globalSettings = settings;
            pokeMap.MapProvider = _globalSettings.Provider;
            _globalSettings.BindNewMapProbider((provider) =>
            {
                pokeMap.MapProvider = provider;
                return true;
            });
        }

        public void addMarker(GMapMarker marker)
        {
            pokeMap.Markers.Add(marker);
        }

        internal void removeMarker(GMapMarker marker)
        {
            pokeMap.Markers.Remove(marker);
        }

        public void FitTheStuff()
        {
            pokeMap.ZoomAndCenterMarkers(null);
        }
    }
}
