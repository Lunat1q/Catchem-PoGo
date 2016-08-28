using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Utils;

namespace PoGo.PokeMobBot.Logic
{
    public static class GoogleRouting
    {
        //Catchem project: https://github.com/Lunat1q/Catchem-PoGo - by Lunat1q
        public static RoutingResponse GetRoute(GeoCoordinate start, GeoCoordinate dest, ISession session, List<GeoCoordinate> waypoints, bool silent = false, bool via = true)
        {
            string apiKey = session.LogicSettings.GoogleDirectionsApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                if (!silent)
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "Google API Key is Empty!"
                    });
                return new RoutingResponse();
            }

            if (waypoints != null && waypoints.Count > 0)
            {
                dest = waypoints.Last();
                waypoints.RemoveAt(waypoints.Count - 1);
            }

            string waypointsRequest = "";
            if (waypoints != null && waypoints.Count > 0)
            {
                waypointsRequest = "&waypoints=optimize:true|";
                var wpList = new List<string>();
                foreach (var wp in waypoints)
                {
                    wpList.Add($"{(via ? "via:" : "")}{wp.Latitude.ToString(CultureInfo.InvariantCulture)},{wp.Longitude.ToString(CultureInfo.InvariantCulture)}");
                }
                waypointsRequest += wpList.Aggregate((x, v) => x + "|" + v);
            }
            try
            {
                Logger.Write("Requesting routing info to Google Directions API", LogLevel.Debug);

                var request = WebRequest.Create(
                  "https://maps.googleapis.com/maps/api/directions/json?" + $"origin={start.Latitude.ToString(CultureInfo.InvariantCulture)},{start.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&destination={dest.Latitude.ToString(CultureInfo.InvariantCulture)},{dest.Longitude.ToString(CultureInfo.InvariantCulture)}" + 
                  waypointsRequest +
                  "&mode=walking" +
                  $"&key={apiKey}");
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Proxy = WebRequest.DefaultWebProxy;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                var responseFromServer = "";
                request.Timeout = 20000;
                using (var response = request.GetResponse())
                {
                    Logger.Write("Got response from Google", LogLevel.Debug);
                    //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                    using (var dataStream = response.GetResponseStream())
                    using (var reader = new StreamReader(dataStream))
                    {
                        responseFromServer = reader.ReadToEnd();
                    }
                }
               
                var googleResponse = JsonConvert.DeserializeObject<GoogleResponse>(responseFromServer); ;// HandleResponse(responseFromServer);

                var responseParsed = new RoutingResponse();
                //var googleCoords = new List<List<double>>();
                var route = googleResponse.routes.FirstOrDefault();
                if (route != null)
                {
                    //var wpOrder = route.waypoint_order;
                    //var legs = googleResponse.routes.FirstOrDefault()?.legs;
                    //if (legs != null)
                    //{
                    //    if (wpOrder != null && wpOrder.Count > 0)
                    //    {
                    //        var orderedLegs = new List<Leg>();
                    //        foreach (int index in wpOrder)
                    //        {
                    //            orderedLegs.Add(legs[index]);
                    //        }
                    //        legs = orderedLegs;
                    //    }
                    //    foreach (var leg in legs)
                    //    {
                    //        foreach (var step in leg.steps)
                    //        {
                    //            googleCoords.Add(new List<double> {step.start_location.lat, step.start_location.lng});
                    //        }
                    //    }
                    //}
                    var testCoords = route.overview_polyline.DecodeToList();
                    responseParsed.Coordinates = testCoords.ToList(); // googleCoords; //
                }
                return responseParsed;
            }
            catch(Exception ex)
            {
                Logger.Write("Routing error: " + ex.Message, LogLevel.Debug);
            }
            RoutingResponse emptyResponse = new RoutingResponse();
            return emptyResponse;  
        }
    }

    public class GoogleResponse
    {
        public List<Route> routes { get; set; }
        public string status { get; set; }
    }

    public class Northeast
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Bounds
    {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Distance
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Duration
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class GoogleLocation
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Distance2
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Duration2
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Polyline
    {
        public string points { get; set; }
    }

    public class Step
    {
        public Distance2 distance { get; set; }
        public Duration2 duration { get; set; }
        public GoogleLocation end_location { get; set; }
        public string html_instructions { get; set; }
        public Polyline polyline { get; set; }
        public GoogleLocation start_location { get; set; }
        public string travel_mode { get; set; }
        public string maneuver { get; set; }
    }

    public class Leg
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public string end_address { get; set; }
        public GoogleLocation end_location { get; set; }
        public string start_address { get; set; }
        public GoogleLocation start_location { get; set; }
        public List<Step> steps { get; set; }
        public List<object> via_waypoint { get; set; }
    }

    public class OverviewPolyline
    {
        public string points { get; set; }

        public IEnumerable<GoogleLocation> Decode()
        {
            return RoutingUtils.DecodePolyline(points);
        }

        public IEnumerable<List<double>> DecodeToList()
        {
            return RoutingUtils.DecodePolylineToList(points);
        }

    }

    public class Route
    {
        public Bounds bounds { get; set; }
        public string copyrights { get; set; }
        public List<Leg> legs { get; set; }
        public OverviewPolyline overview_polyline { get; set; }
        public string summary { get; set; }
        public List<object> warnings { get; set; }
        public List<object> waypoint_order { get; set; }
    }

}
