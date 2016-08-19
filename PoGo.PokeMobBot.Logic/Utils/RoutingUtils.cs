using System;
using System.Collections.Generic;
using System.Linq;
using GeoCoordinatePortable;

namespace PoGo.PokeMobBot.Logic.Utils
{
    public static class RoutingUtils
    {
        public static List<FortCacheItem> GetBestRoute(FortCacheItem startingPokestop,
            IEnumerable<FortCacheItem> pokestopsList, int amountToVisit)
        {
            var routingEngine = new RouteEngine(pokestopsList);
            var route = routingEngine.CalculateMinCost(startingPokestop, amountToVisit).Keys.ToList();
            return route;
        }
    }

    public class RouteEngine
    {
        public IEnumerable<FortCacheItem> Locations { get; set; }

        public List<Connection> Connections { get; set; }

        public RouteEngine(IEnumerable<FortCacheItem> locs)
        {
            Connections = new List<Connection>();
            Locations = locs.ToList();
            foreach (var loc in Locations)
            {
                var allOthersLocations = Locations.Where(x => x != loc);
                foreach (var otherLoc in allOthersLocations)
                {
                    Connections.Add(new Connection(loc, otherLoc));
                }
            }
        }

        /// <summary>
        /// Calculates the shortest route to all the other pokestops
        /// </summary>
        /// <param name="startLocation">starting pokestop</param>
        /// <param name="amountToVisit">now many you want to visit</param>
        /// <returns>List of all locations and their shortest route</returns>
        public Dictionary<FortCacheItem, Route> CalculateMinCost(FortCacheItem startLocation, int amountToVisit)
        {
            //Initialise a new empty route list
            //Initialise a new empty handled locations list
            var handledLocations = new List<FortCacheItem>();

            //Initialise the new routes. the constructor will set the route Distance to in.max
            var shortestPaths = Locations.ToDictionary(location => location, location => new Route(location.Id));

            //The startPosition has a Distance 0. 
            shortestPaths[startLocation].Cost = 0;


            //If all locations are handled, stop the engine and return the result
            while (handledLocations.Count <= amountToVisit)
            {
                //Order the locations
                var shortestLocations = (from s in shortestPaths
                                         orderby s.Value.Cost
                                         select s.Key).ToList();


                FortCacheItem locationToProcess = null;

                //Search for the nearest location that isn't handled
                foreach (var location in shortestLocations)
                {
                    if (!handledLocations.Contains(location))
                    {
                        //If the cost equals int.max, there are no more possible connections to the remaining locations
                        if (Math.Abs(shortestPaths[location].Cost - float.MaxValue) < 0.1)
                            return shortestPaths;
                        locationToProcess = location;
                        break;
                    }
                }

                //Select all connections where the startposition is the location to Process
                var selectedConnections = from c in Connections
                                          where c.A == locationToProcess
                                          select c;

                //Iterate through all connections and search for a connection which is shorter
                foreach (var conn in selectedConnections)
                {
                    if (shortestPaths[conn.B].Cost <= conn.Distance + shortestPaths[conn.A].Cost) continue;
                    shortestPaths[conn.B].Connections = shortestPaths[conn.A].Connections.ToList();
                    shortestPaths[conn.B].Connections.Add(conn);
                    shortestPaths[conn.B].Cost = conn.Distance + shortestPaths[conn.A].Cost;
                }
                //Add the location to the list of processed locations
                handledLocations.Add(locationToProcess);
            }


            return shortestPaths;
        }
    }

    public class Route
    {
        private readonly string _identifier;

        public Route(string identifier)
        {
            Cost = float.MaxValue;
            Connections = new List<Connection>();
            _identifier = identifier;
        }

        public List<Connection> Connections { get; set; }

        public float Cost { get; set; }

        public override string ToString()
        {
            return "Id:" + _identifier + " Cost:" + Cost;
        }
    }

    public class Connection
    {
        public Connection(FortCacheItem a, FortCacheItem b)
        {
            A = a;
            B = b;
            Distance = (float)LocationUtils.CalculateDistanceInMeters(new GeoCoordinate(a.Latitude, a.Longitude),
                new GeoCoordinate(b.Latitude, b.Longitude));
        }

        public FortCacheItem B { get; set; }

        public FortCacheItem A { get; set; }

        public float Distance { get; internal set; }
    }
}
