using Newtonsoft.Json.Linq;
using PoGo.PokeMobBot.Logic.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;

namespace PoGo.PokeMobBot.Logic.API
{
    public class GeoLatLonAlt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Alt { get; set; }
    }
    // ReSharper disable once InconsistentNaming
    public class MapzenAPI
    {
        private static List<GeoLatLonAlt> _knownAltitude = new List<GeoLatLonAlt>();
        private static string _jsonFile = "Altitude-data.json";
        private static bool _loaded;

        public static void LoadKnownCoords()
        {
            if (_loaded) return;
            _loaded = true;
            var mapZenDir = Path.Combine(Directory.GetCurrentDirectory(), "MapzenAPI");
            if (!Directory.Exists(mapZenDir))
                Directory.CreateDirectory(mapZenDir);
            _knownAltitude = SerializeUtils.DeserializeDataJson<List<GeoLatLonAlt>>(Path.Combine(mapZenDir, _jsonFile)) ?? new List<GeoLatLonAlt>();
        }

        public static void SaveKnownCoords()
        {
            var mapZenDir = Path.Combine(Directory.GetCurrentDirectory(), "MapzenAPI");
            if (!Directory.Exists(mapZenDir))
                Directory.CreateDirectory(mapZenDir);

            var dataToSave = _knownAltitude;
            dataToSave.SerializeDataJson(Path.Combine(mapZenDir, _jsonFile));
        }

        private static readonly Random R = new Random();
        protected string Api = "https://elevation.mapzen.com/height";
        protected string[] Options = { "?json={\"shape\":[{\"lat\":", ",\"lon\":", "}]}&api_key=" };
        private ISession _session;
        public string ApiKey = "";

        public void SetSession(ISession session)
        {
            if (session == null) return;
            _session = session;
            _session.MapzenApi = this;

        }

        public MapzenAPI()
        {
            LoadKnownCoords();
        }

        protected string Url(string lat, string lon, string key)
        {
            return Api + Options[0] + lat + Options[1] + lon + Options[2] + key;
        }
        protected string Request(string httpUrl)
        {
            try
            {
                var get = "";
                var request = (HttpWebRequest)WebRequest.Create(httpUrl);
                request.Proxy = _session == null ? WebRequest.GetSystemWebProxy() : _session.Proxy;
                if (_session == null)
                    request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                request.AutomaticDecompression = DecompressionMethods.GZip;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                    if (stream != null)
                        using (var reader = new StreamReader(stream))
                        {
                            get = reader.ReadToEnd();
                        }
                var json = JObject.Parse(get);
#if DEBUG
                Logger.Write(json.ToString(), LogLevel.Debug);
                Logger.Write("Altitude: " + json["height"][0], LogLevel.Debug);
#endif
                return (string)json["height"][0];
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR: " + ex.Message, LogLevel.Error);
                return "ERROR";
            }

        }
        protected double GetHeight(string[] data)
        {
            if (data[2].Equals("ERROR"))
            {
                Logger.Write("There was an error grabbing Altitude from Mapzen API! Check your Elevation API Key!",
                    LogLevel.Warning);
                return _session != null ? R.NextInRange(_session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
            }
            Logger.Write("Successfully grabbed new Mapzen Elevation: " + data[2] + " Meters.");
            var latLonAlt = new GeoLatLonAlt()
            {
                Lat = double.Parse(data[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                Lon = double.Parse(data[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                Alt = double.Parse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture) + 0.8 + Math.Round(R.NextInRange(0, 0.2), 7)
            };
            _knownAltitude.Add(latLonAlt);
            return latLonAlt.Alt;
        }
        public bool CheckForExistingAltitude(double lat, double lon)
        {
            return _knownAltitude.Any(x => LocationUtils.CalculateDistanceInMeters(x.Lat, x.Lon, lat, lon) < 5);
        }

        public double GetExistingAltitude(double lat, double lon)
        {
            //trying to find points closer then 5m from search loc
            return _knownAltitude.FirstOrDefault(x => LocationUtils.CalculateDistanceInMeters(x.Lat, x.Lon, lat, lon) < 5)?.Alt ?? R.NextInRange(_session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax);
        }

        public double GetAltitude(double lat, double lon, string key = "")
        {
            Logger.Write("Using MapzenAPI to obtian Altitude based on Longitude and Latitude.");
            if (key != "") ApiKey = key;

            if (CheckForExistingAltitude(lat, lon))
            {
                return GetExistingAltitude(lat, lon);
            }
            if (!Equals(lat, default(double)) && !Equals(lon, default(double)) && !ApiKey.Equals(""))
            {
                return GetHeight(new[]
                {
                    lat.ToString(CultureInfo.InvariantCulture),
                    lon.ToString(CultureInfo.InvariantCulture),
                    Request(Url(lat.ToString(CultureInfo.InvariantCulture), lon.ToString(CultureInfo.InvariantCulture),
                        key))
                });
            }
            return _session != null ? R.NextInRange(_session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
        }
    }

}
