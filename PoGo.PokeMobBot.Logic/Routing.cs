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

<<<<<<< HEAD
=======
            string apiKey = session.LogicSettings.MobBotRoutingApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "MobBotRouting API Key is Empty!"
                });
                return new RoutingResponse();
            }
>>>>>>> e1442858da0e69186d705026e2f9f170af88b305
            try
            {
                Logger.Write("Requesting routing info from localhost", LogLevel.Debug);
                var request = WebRequest.Create(
                $"http://localhost:5000" + $"/route/v1/foot/{start.Longitude.ToString(CultureInfo.InvariantCulture)},{start.Latitude.ToString(CultureInfo.InvariantCulture)};{dest.Longitude.ToString(CultureInfo.InvariantCulture)},{dest.Latitude.ToString(CultureInfo.InvariantCulture)}?geometries=geojson");
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Proxy = WebRequest.DefaultWebProxy;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                var responseFromServer = "";
                request.Timeout = 20000;
                using (var response = request.GetResponse())
                {
                    Logger.Write("Got response from localhost", LogLevel.Debug);
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
            var emptyResponse = new RoutingResponse {};
            return emptyResponse;  
        }
    }

    public class RoutingResponse
    {
        public List<Route2> routes { get; set; }
        public string code { get; set; }
    }

    public class Geometry
    {
        public string type { get; set; }
        public List<List<double>> coordinates { get; set; }
    }

    public class Route2
    {
        public double distance { get; set; }
        public double duration { get; set; }
        public Geometry geometry { get; set; }
    }


}
