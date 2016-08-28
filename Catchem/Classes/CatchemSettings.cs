﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GMap.NET.MapProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Classes
{
    public class CatchemSettings
    {
        private const string FilePath = "catchem.json";

        [JsonIgnore]
        public GMapProvider Provider = GMapProviders.GoogleMap;

        [JsonIgnore]
        private readonly List<Func<GMapProvider, bool>> _mapAutoSetFuncs = new List<Func<GMapProvider, bool>>();

        
        public MapProvider ProviderEnum = MapProvider.Google;

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

        public void BindNewMapProbider(Func<GMapProvider, bool> binder)
        {
            _mapAutoSetFuncs.Add(binder);
        }

        public void Save()
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
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
                var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
                if (File.Exists(settingsPath))
                {
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

                    var input = File.ReadAllText(FilePath);

                    JsonConvert.PopulateObject(input, this, jsonSettings);
                    LoadProperProvider();
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
