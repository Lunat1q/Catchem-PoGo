
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;

namespace PoGo.PokeMobBot.Logic.API
{
    public class MapzenRouting
    {
        public static RoutingResponse GetRoute(GeoCoordinate start, GeoCoordinate dest, ISession session, List<GeoCoordinate> waypoints, bool silent = false)
        {
            string apiKey = session.LogicSettings.MapzenValhallaApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                if (!silent)
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "Mapzen Valhalla API Key is Empty!"
                    });
                return new RoutingResponse();
            }

            if (waypoints == null || waypoints.Count == 0)
            {
                waypoints = new List<GeoCoordinate> {dest};
            }
            waypoints.Insert(0, start);

            string waypointsRequest = "";
            if (waypoints.Count > 0)
            {
                waypointsRequest = "\"locations\":[";
                var wpList = new List<string>();
                foreach (var wp in waypoints)
                {
                    wpList.Add($"{{\"lat\":{wp.Latitude.ToString(CultureInfo.InvariantCulture)},\"lon\":{wp.Longitude.ToString(CultureInfo.InvariantCulture)},\"type\":\"break\"}}");
                }
                waypointsRequest += wpList.Aggregate((x, v) => x + "," + v);
                waypointsRequest += "],";
            }
            try
            {
                Logger.Write("Requesting routing info to Mapzen Valhalla Routing API", LogLevel.Debug);

                var request = WebRequest.Create(
                  "https://valhalla.mapzen.com/route?json={" + waypointsRequest +
                  "\"costing\":\"pedestrian\",\"costing_options\":{\"pedestrian\":{\"alley_factor\":1.0, \"driveway_factor\":1.0, \"step_penalty\":1.0}}}" +
                  $"&api_key={apiKey}");
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Proxy = WebRequest.DefaultWebProxy;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                var responseFromServer = "";
                request.Timeout = 20000;
                using (var response = request.GetResponse())
                {
                    Logger.Write("Got response from MapzenValhalla", LogLevel.Debug);
                    //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                    using (var dataStream = response.GetResponseStream())
                    using (var reader = new StreamReader(dataStream))
                    {
                        responseFromServer = reader.ReadToEnd();
                    }
                }

                var valhallaResponse = JsonConvert.DeserializeObject<ValhallaResponse>(responseFromServer);

                var responseParsed = new RoutingResponse();
                var route = valhallaResponse.trip;
                if (route != null)
                {
                    responseParsed.Coordinates = route.GetRoute();
                }
                return responseParsed;
            }
            catch (Exception ex)
            {
                Logger.Write("Routing error: " + ex.Message, LogLevel.Debug);
            }
            RoutingResponse emptyResponse = new RoutingResponse();
            return emptyResponse;
        }
    }

    public class ValhallaResponse
    {
        public Trip trip;
    }

    public class Trip
    {
        public string status;
        public string status_message;
        public string units;
        public string language;
        public MapZenLocation[] locations;
        public Legs[] legs;

        public List<List<double>> GetRoute()
        {
            List<List<double>> result = new List<List<double>>();
            foreach (var leg in legs)
            {
                result.AddRange(leg.DecodeToList());
            }
            return result;
        }
    }

    public class MapZenLocation
    {
        public string side_of_street;
        public double lat;
        public double lon;
        public string type;
    }

    public class Legs
    {
        public string shape;
        public Summary summary;
        public Maneuvers[] maneuvers;

        public IEnumerable<List<double>> DecodeToList()
        {
            return RoutingUtils.DecodePolylineToList(shape, 6);
        }
    }

    public class Summary
    {
        public string max_lon;
        public string max_lat;
        public string time;
        public string length;
        public string min_lat;
        public string min_lon;
    }

    public class Maneuvers
    {
        public string travel_type;
        public string travel_mode;
        public string verbal_multi_cue;
        public string verbal_pre_transition_instruction;
        public string verbal_transition_alert_instruction;
        public string length;
        public string instruction;
        public string end_shape_index;
        public string type;
        public string time;
        public string verbal_post_transition_instruction;
        public string begin_shape_index;
        public string rough;
    }
}

