using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using GMap.NET.MapProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Classes
{
    public class CatchemSettings
    {
        [JsonIgnore]
        private const string FilePath = "catchem.json";
        [JsonIgnore]
        private const string ConfFolder = "Config";

        [JsonIgnore]
        public GMapProvider Provider = GMapProviders.GoogleMap;

        [JsonIgnore]
        private readonly List<Func<GMapProvider, bool>> _mapAutoSetFuncs = new List<Func<GMapProvider, bool>>();

        public ObservableCollection<BotRoute> Routes = new ObservableCollection<BotRoute>();
        
        public MapProvider ProviderEnum = MapProvider.Google;

        public string UiLanguage = "English";

        public int ConsoleRowsToShow = 100;
        public int HeatMapClusterRadius = 30;
        public bool KeepPokeMarkersOnMap;
        public bool FollowTheWhiteRabbit;

        public void LoadProperProvider()
        {
            switch (ProviderEnum)
            {
                case MapProvider.Google:
                    Provider = GMapProviders.GoogleMap;
                    break;
                //case MapProvider.Osm:
                //    Provider = GMapProviders.OpenStreetMap;
                //    break;
                case MapProvider.ArcGis:
                    Provider = GMapProviders.ArcGIS_World_Street_Map;
                    break;
                case MapProvider.Ocl:
                    Provider = GMapProviders.OpenCycleMap;
                    break;
                case MapProvider.Yandex:
                    Provider = GMapProviders.YandexMap;
                    break;
                case MapProvider.Bing:
                    Provider = GMapProviders.BingMap;
                    break;
                case MapProvider.OviMap:
                    Provider = GMapProviders.OviMap;
                    break;
                case MapProvider.YandexHybrid:
                    Provider = GMapProviders.YandexHybridMap;
                    break;
                default:
                    Provider = GMapProviders.GoogleMap;
                    break;
            }

            foreach (var func in _mapAutoSetFuncs)
            {
               func(Provider);
            }
        }

        public void BindNewMapProvider(Func<GMapProvider, bool> binder)
        {
            _mapAutoSetFuncs.Add(binder);
        }

        public void Save()
        {
            //var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ConfFolder, FilePath);
            //this.SerializeDataJson(settingsPath);
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ConfFolder, FilePath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
            jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

            this.SerializeDataJson(settingsPath);
        }

        public void Load()
        {
            try
            {
                var oldSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
                var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ConfFolder, FilePath);
                if (File.Exists(oldSettingsPath))
                {
                    if (File.Exists(settingsPath))
                    {
                        File.Move(settingsPath, settingsPath + ".bak");
                    }
                    File.Move(oldSettingsPath, settingsPath);
                    Task.Delay(1000);
                }

                if (File.Exists(settingsPath))
                {
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

                    var input = File.ReadAllText(settingsPath);

                    JsonConvert.PopulateObject(input, this, jsonSettings);
                    LoadProperProvider();
                    Routes = Routes == null ? new ObservableCollection<BotRoute>() : new ObservableCollection<BotRoute>(Routes.OrderBy(x => x.Name));
                    if (ConsoleRowsToShow < 1)
                        ConsoleRowsToShow = 100;
                    if (HeatMapClusterRadius < 1)
                        HeatMapClusterRadius = 30;
                    if (UiLanguage == null)
                        UiLanguage = "English";
                }
                else
                {
                    Save();
                }
            }
            catch (Exception)
            {
                Save();
            }
        }
    }

    public class BotRoute : CatchemNotified
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public override string ToString()
        {
            return _name;
        }

        public List<GeoCoordinate> InitialWp;

        [JsonIgnore]
        public int WpCount => Route.RoutePoints.Count;

        public CustomRoute Route = new CustomRoute();
    }


    public enum MapProvider
    {
        Google,
        //Osm,
        ArcGis,
        Ocl,
        Yandex,
        Bing,
        OviMap,
        YandexHybrid
    }
}
