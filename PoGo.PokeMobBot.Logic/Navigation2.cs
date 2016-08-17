#region using directives

#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using System.Collections.Generic;
using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Tasks;

#endregion

// ReSharper disable RedundantAssignment

#endregion



namespace PoGo.PokeMobBot.Logic
{
    public class HumanNavigation
    {
        private readonly Client _client;
        public HumanNavigation(Client client)
        {
            _client = client;
        }


        public async Task<PlayerUpdateResponse> MoveEH(GeoCoordinate destination, double walkingSpeedInKilometersPerHour,
            Func<Task<bool>> functionExecutedWhileWalking, Func<Task<bool>> functionExecutedWhileWalking2,
            CancellationToken cancellationToken, ISession session)
        {
            GeoCoordinate currentLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude, _client.CurrentAltitude);
            PlayerUpdateResponse result = new PlayerUpdateResponse();
            List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
            var routingResponse = Routing.GetRoute(currentLocation, destination);
            foreach (var item in routingResponse.Coordinates)
            {
                //0 = lat, 1 = long (MAYBE NOT THO?)
                waypoints.Add(new GeoCoordinate(item.ToArray()[1], item.ToArray()[0]));
            }
            Navigation navi = new Navigation(_client, UpdatePositionEvent);
            for (var x = 0; x < waypoints.Count; x++)
            {
                var nextSpeed = session.Client.rnd.NextInRange(session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax) * session.Settings.MoveSpeedFactor;
                await navi.HumanPathWalking(waypoints.ToArray()[x], nextSpeed,
                    functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                UpdatePositionEvent?.Invoke(waypoints.ToArray()[x].Latitude, waypoints.ToArray()[x].Longitude, waypoints.ToArray()[x].Altitude);
                //Console.WriteLine("Hit waypoint " + x);
            }
            var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
            if (!(LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)) return result;
            {
                var nextSpeed = session.Client.rnd.NextInRange(session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax) * session.Settings.MoveSpeedFactor;
                await navi.HumanPathWalking(destination, nextSpeed,
                    functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
            }
            return result;
        }

        public async Task<PlayerUpdateResponse> Move(GeoCoordinate destination, double walkingSpeedMin, double walkingSpeedMax, Func<Task<bool>> functionExecutedWhileWalking, Func<Task<bool>> functionExecutedWhileWalking2,
            CancellationToken cancellationToken, ISession session, bool direct = false)
        {
            var currentLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude, _client.CurrentAltitude);
            var result = new PlayerUpdateResponse();
            if (session.LogicSettings.UseHumanPathing)
            {
                ////initial coordinate generaton

                ////prepare the result object for further manipulation + return


                ////initial time
                //var requestSendDateTime = DateTime.Now;
                //var distanceToDest = LocationUtils.CalculateDistanceInMeters(currentLocation, destination);
                //double metersPerInterval = 0.5; //approximate meters for each interval/waypoint to be spaced from the last.
                //////get distance ofc
                //////create segments
                //var segments = Math.Floor(distanceToDest / metersPerInterval);
                List<GeoCoordinate> waypoints = new List<GeoCoordinate>();
                ////get differences in lat / long
                //var latDiff = Math.Abs(currentLocation.Latitude - destination.Latitude);
                //var lonDiff = Math.Abs(currentLocation.Longitude - destination.Longitude);
                //var latAdd = latDiff / segments;
                //var lonAdd = latDiff / segments;
                //var lastLat = currentLocation.Latitude;
                //var lastLon = currentLocation.Longitude;
                ////generate waypoints old code -goes in straight line basically
                //for (int i = 0; i < segments; i++)
                //{
                //    //TODO: add altitude calculations into everything
                //    lastLat += latAdd;
                //    lastLon += lonAdd;
                //    waypoints.Add(new GeoCoordinate(lastLat, lastLon, currentLocation.Altitude));
                //}

                //TODO: refactor the generation of waypoint code to break the waypoints given to us by the routing information down into segements like above.
                //generate waypoints new code
                if (!direct)
                {
                    //var routingResponse = OsmRouting.GetRoute(currentLocation, destination, session);
                    //waypoints = routingResponse.Coordinates;
					RoutingResponse routingResponse;
	                try
	                {
                        routingResponse = !session.LogicSettings.UseOpenLsRouting ? Routing.GetRoute(currentLocation, destination) : OsmRouting.GetRoute(currentLocation, destination, session);
                    }
	                catch (NullReferenceException ex)
	                {
	                    session.EventDispatcher.Send(new DebugEvent
	                    {
	                        Message = ex.ToString()
	                    });
	                    routingResponse = new RoutingResponse();
	                }
                    var nextPath = routingResponse?.Coordinates?.Select(item => Tuple.Create(item[1], item[0])).ToList();
                    session.EventDispatcher.Send(new NextRouteEvent
                    {
                        Coords = nextPath
	                });
                    if (routingResponse?.Coordinates != null)
					    foreach (var item in routingResponse.Coordinates)
	                    {
                            //0 = lat, 1 = long (MAYBE NOT THO?)
	                        waypoints.Add(!session.LogicSettings.UseOpenLsRouting
	                            ? new GeoCoordinate(item.ToArray()[1], item.ToArray()[0],
                                    session.LogicSettings.UseMapzenApiElevation ? session.MapzenApi.GetAltitude(item.ToArray()[1], item.ToArray()[0]) : 0)
	                            : new GeoCoordinate(item.ToArray()[1], item.ToArray()[0], item.ToArray()[2]));
	                    }
                }

                if (waypoints.Count == 0)
                    waypoints.Add(destination);

                //var timeSinceMoveStart = DateTime.Now.Ticks;
                //double curAcceleration = 1.66; //Lets assume we accelerate at 1.66 m/s ish. TODO: Fuzz this a bit
                //double curWalkingSpeed = 0;
                //double maxWalkingSpeed = (session.LogicSettings.WalkingSpeedInKilometerPerHour / 3.6); //Get movement speed in meters

                ////TODO: Maybe update SensorInfo to replicate/mimic movement, or is it fine as is?
                //bool StopWalking = false;
                //double TimeToAccelerate = GetAccelerationTime(curWalkingSpeed, maxWalkingSpeed, curAcceleration);
                ////double InitialMove = getDistanceTraveledAccelerating(TimeToAccelerate, curAcceleration, curWalkingSpeed);


                //double MoveLeft = curWalkingSpeed;
                //bool NeedMoreMove = false;
                //bool StopMove = false;
                //int UpdateInterval = 1; // in seconds - any more than this breaks the calculations for distance and such. It all relys on taking ~1 second to perform the actions needed, pretty much.

                //makes you appear to move slower if you're catching pokemon, hitting stops, etc.
                //This feels like more human behavior. Dunnomateee
                Navigation navi = new Navigation(_client, UpdatePositionEvent);
                var waypointsArr = waypoints.ToArray();
                //MILD REWRITE TO USE HUMANPATHWALKING;
                foreach (var t in waypointsArr)
                {
                    if (session.ForceMoveTo != null)
                    {
                        return await ForceMoveTask.Execute(session, cancellationToken);
                    }
                    // skip waypoints under 5 meters
                    var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                    var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation,
                        t);
                    if (distanceToTarget <= 5)
                        continue;

                    var nextSpeed = session.Client.rnd.NextInRange(walkingSpeedMin, walkingSpeedMax) * session.Settings.MoveSpeedFactor;

                    result = await
                        navi.HumanPathWalking(t, nextSpeed,
                            functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                    if (RuntimeSettings.BreakOutOfPathing)
                        return result;
                    UpdatePositionEvent?.Invoke(t.Latitude, t.Longitude, t.Altitude);
                    //Console.WriteLine("Hit waypoint " + x);
                }
                var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
                if (LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)
                {
                    var nextSpeed = session.Client.rnd.NextInRange(walkingSpeedMin, walkingSpeedMax) * session.Settings.MoveSpeedFactor;

                    result = await navi.HumanPathWalking(destination, nextSpeed,
                        functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                }
            }
            else
            {
                if (destination.Latitude.Equals(RuntimeSettings.lastPokeStopCoordinate.Latitude) &&
                    destination.Longitude.Equals(RuntimeSettings.lastPokeStopCoordinate.Longitude))
                    RuntimeSettings.BreakOutOfPathing = true;

                if (RuntimeSettings.BreakOutOfPathing)
                {
                    await MaintenanceTask.Execute(session, cancellationToken);
                    return result;
                }
                var navi = new Navigation(_client, UpdatePositionEvent);
                var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
                if (LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)
                {
                    var nextSpeed = session.Client.rnd.NextInRange(walkingSpeedMin, walkingSpeedMax) * session.Settings.MoveSpeedFactor;

                    result = await navi.HumanPathWalking(destination, nextSpeed,
                        functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                }
            }
            await MaintenanceTask.Execute(session, cancellationToken);
            return result;
        }

        public static double GetAccelerationTime(double curV, double maxV, double acc)
        {
            if (Math.Abs(acc) < 0.001)
                return 9001;
            else
                return (maxV - curV) / acc;
        }

        public static double GetDistanceTraveledAccelerating(double time, double acc, double curV)
        {
            return ((curV * time) + ((acc * Math.Pow(time, 2)) / 2));
        }

        public event UpdatePositionDelegate UpdatePositionEvent;
    }
}
