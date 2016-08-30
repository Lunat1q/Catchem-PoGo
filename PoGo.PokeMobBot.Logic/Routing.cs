using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using GeoCoordinatePortable;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;

namespace PoGo.PokeMobBot.Logic
{
    public static class Routing
    {
        public static RoutingResponse GetRoute(GeoCoordinate start, GeoCoordinate dest, ISession session)
        {

            string apiKey = session.LogicSettings.MobBotRoutingApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "MobBotRouting API Key is Empty!"
                });
                return new RoutingResponse();
            }
            try
            {
                Logger.Write("Requesting routing info from MobRouting.com", LogLevel.Debug);
                var request = WebRequest.Create(
                  $"http://mobrouting.com" + $"/api/dev/gosmore.php?format=geojson&apikey={apiKey}&flat={start.Latitude.ToString(CultureInfo.InvariantCulture)}&flon={start.Longitude.ToString(CultureInfo.InvariantCulture)}&tlat={dest.Latitude.ToString(CultureInfo.InvariantCulture)}&tlon={dest.Longitude.ToString(CultureInfo.InvariantCulture)}&v=foot&fast=1&layer=mapnik");
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Proxy = WebRequest.DefaultWebProxy;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                var responseFromServer = "";
                request.Timeout = 20000;
                using (var response = request.GetResponse())
                {
                    Logger.Write("Got response from www.mobrouting.com", LogLevel.Debug);
                    //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                    using (var dataStream = response.GetResponseStream())
                    using (var reader = new StreamReader(dataStream))
                    {
                        responseFromServer = reader.ReadToEnd();
                    }
                }
                //Console.WriteLine(responseFromServer);
                var responseParsed = JsonConvert.DeserializeObject<RoutingResponse>(responseFromServer);

                return responseParsed;
            }
            catch(Exception ex)
            {
                Logger.Write("Routing error: " + ex.Message, LogLevel.Debug);
            }
            var emptyResponse = new RoutingResponse {Coordinates = new List<List<double>>()};
            return emptyResponse;  
        }
    }

    public class RoutingResponse
    {
        public string Type { get; set; }
        public Crs Crs { get; set; }
        public List<List<double>> Coordinates { get; set; }
        public Properties2 Properties { get; set; }
    }
    public class Properties
    {
        public string name { get; set; }
    }

    public class Crs
    {
        public string type { get; set; }
        public Properties properties { get; set; }
    }

    public class Properties2
    {
        public string distance { get; set; }
        public string description { get; set; }
        public string traveltime { get; set; }
    }

    
}
