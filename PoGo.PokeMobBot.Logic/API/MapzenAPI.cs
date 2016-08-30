using Newtonsoft.Json.Linq;
using PoGo.PokeMobBot.Logic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using RandomExtensions = PokemonGo.RocketAPI.Extensions.RandomExtensions;

namespace PoGo.PokeMobBot.Logic.API
{
    public class GeoLatLonAlt
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Alt { get; set; }
    }
    // ReSharper disable once InconsistentNaming
    public class MapzenAPI //Improved by Catchem project: https://github.com/Lunat1q/Catchem-PoGo - by Lunat1q
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
        protected async Task<string> Request(string httpUrl)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                var get = "";
                var request = (HttpWebRequest) WebRequest.Create(httpUrl);
                request.Proxy = _session == null ? WebRequest.GetSystemWebProxy() : _session.Proxy;
                if (_session == null)
                    request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                request.AutomaticDecompression = DecompressionMethods.GZip;
                using (var response = (HttpWebResponse) request.GetResponse())
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
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds < 1000)
                    await Task.Delay(1000 - (int) stopWatch.ElapsedMilliseconds);
                return (string) json["height"][0];
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR: " + ex.Message, LogLevel.Error);
                return "ERROR";
            }
            finally
            {
                if (stopWatch.IsRunning)
                    stopWatch.Stop();
            }

        }

        protected async Task<List<double>> RequestHeights(List<GeoCoordinate> points)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var resList = new List<double>();
            try
            {
                var httpUrl = PrepareUrlForList(points);

                var get = "";
                var request = (HttpWebRequest)WebRequest.Create(httpUrl);
                request.Proxy = _session == null ? WebRequest.GetSystemWebProxy() : _session.Proxy;
                if (_session == null)
                    request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Timeout = 60000;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                    if (stream != null)
                        using (var reader = new StreamReader(stream))
                        {
                            get = reader.ReadToEnd();
                        }
                var json = JObject.Parse(get);
                stopWatch.Stop();
                if (stopWatch.ElapsedMilliseconds < 10000)
                    await Task.Delay(10000 - (int)stopWatch.ElapsedMilliseconds);
                
                foreach (var h in json["height"])
                {
                    if (h.ToString().Equals("ERROR"))
                    {
                        resList.Add(_session != null ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120));
                    }
                    double hVal = 0;
                    if (!double.TryParse(h.ToString(), out hVal))
                    {
                        hVal = _session != null
                            ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) + 0.8 + Math.Round(RandomExtensions.NextInRange(R, 0, 0.2), 5)
                            : R.Next(10, 120);
                    }
                    resList.Add(hVal);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR: " + ex.Message, LogLevel.Error);
                return resList;
            }
            finally
            {
                if (stopWatch.IsRunning)
                    stopWatch.Stop();
            }
            while (points.Count < resList.Count)
            {
                resList.Add(_session != null
                    ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax)
                    : R.Next(10, 120));
            }
            return resList;
        }

        private string PrepareUrlForList(List<GeoCoordinate> points)
        {
            string coordsPart = "";
            var coordsJson = new List<string>();
            foreach (var point in points)
            {
                coordsJson.Add($"{{\"lat\":{point.Latitude.ToString(CultureInfo.InvariantCulture)},\"lon\":{point.Longitude.ToString(CultureInfo.InvariantCulture)}}}");
            }
            coordsPart = coordsJson.Aggregate((x, v) => x + "," + v);
            return "https://elevation.mapzen.com/height?json={\"shape\":[" + coordsPart + "]}&api_key=" + ApiKey;

        }

        protected double GetHeight(string[] data)
        {
            if (data[2].Equals("ERROR"))
            {
                Logger.Write("There was an error grabbing Altitude from Mapzen API! Check your Elevation API Key!",
                    LogLevel.Warning);
                return _session != null ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
            }
            Logger.Write("Successfully grabbed new Mapzen Elevation: " + data[2] + " Meters.");
            var latLonAlt = new GeoLatLonAlt()
            {
                Lat = double.Parse(data[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                Lon = double.Parse(data[1], NumberStyles.Any, CultureInfo.InvariantCulture),
                Alt = double.Parse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture) + 0.8 + Math.Round(RandomExtensions.NextInRange(R, 0, 0.2), 7)
            };
            _knownAltitude.Add(latLonAlt);
            return latLonAlt.Alt;
        }
        public bool CheckForExistingAltitude(double lat, double lon)
        {
            var knownTemp = _knownAltitude.ToArray();
            return knownTemp.Any(x => LocationUtils.CalculateDistanceInMeters(x.Lat, x.Lon, lat, lon) < 30);
        }

        public double GetExistingAltitude(double lat, double lon)
        {
            //trying to find points closer then 5m from search loc
            return _knownAltitude.FirstOrDefault(x => LocationUtils.CalculateDistanceInMeters(x.Lat, x.Lon, lat, lon) < 30)?.Alt ?? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax);
        }

        public double GetAltitudeSync(double lat, double lon, string key = "")
        {
            return AsyncHelpers.RunSync(() => GetAltitude(lat, lon, key));
        }

        public async Task<double> GetAltitude(double lat, double lon, string key = "")
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
                    await Request(Url(lat.ToString(CultureInfo.InvariantCulture), lon.ToString(CultureInfo.InvariantCulture),
                        key))
                });
            }
            return _session != null ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
        }

        public async Task<List<GeoCoordinate>> FillAltitude(List<GeoCoordinate> points, string key = "")
        {
            Logger.Write("Using MapzenAPI to obtian Altitude based on Longitude and Latitude.");
            if (key != "") ApiKey = key;

            if (!ApiKey.Equals(""))
            {
                List<GeoCoordinate> pointsToRequest = new List<GeoCoordinate>();
                foreach (var point in points)
                {
                    if (pointsToRequest.Any( x=> LocationUtils.CalculateDistanceInMeters(x, point) <= 30)) continue;
                    if (CheckForExistingAltitude(point.Latitude, point.Longitude)) continue;
                    
                    pointsToRequest.Add(point);
                }
                var heights = await RequestHeights(pointsToRequest);
                for (int index = 0; index < pointsToRequest.Count; index++)
                {
                    if (index < heights.Count)
                    {
                        pointsToRequest[index].Altitude = heights[index];
                        _knownAltitude.Add(new GeoLatLonAlt()
                        {
                            Lat = pointsToRequest[index].Latitude,
                            Lon = pointsToRequest[index].Longitude,
                            Alt = pointsToRequest[index].Altitude
                        });
                    }
                }
                foreach (var point in points)
                {
                    if (CheckForExistingAltitude(point.Latitude, point.Longitude))
                    {
                        point.Altitude = GetExistingAltitude(point.Latitude, point.Longitude);
                    }
                    else
                    {
                        point.Altitude = _session != null ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
                    }
                }
            }
            else
            {
                foreach (var point in points)
                {
                    point.Altitude = _session != null ? RandomExtensions.NextInRange(R, _session.Settings.DefaultAltitudeMin, _session.Settings.DefaultAltitudeMax) : R.Next(10, 120);
                }
            }
            return points;
        }
    }

}
