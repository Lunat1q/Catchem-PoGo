using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Windows.Controls;
using System.Windows.Input;
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
            _globalSettings.BindNewMapProvider((provider) =>
            {
                pokeMap.MapProvider = provider;
                return true;
            });
        }

        public void AddMarker(GMapMarker marker)
        {
            pokeMap.Markers.Add(marker);
        }

        internal void RemoveMarker(GMapMarker marker)
        {
            pokeMap.Markers.Remove(marker);
        }

        public void FitTheStuff()
        {
            pokeMap.ZoomAndCenterMarkers(null);
        }
    }
}
