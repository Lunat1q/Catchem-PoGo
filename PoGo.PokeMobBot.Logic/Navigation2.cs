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

        public async Task<PlayerUpdateResponse> Move(GeoCoordinate destination, double walkingSpeedMin, double walkingSpeedMax, Func<Task<bool>> functionExecutedWhileWalking, Func<Task<bool>> functionExecutedWhileWalking2,
            CancellationToken cancellationToken, ISession session)
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
                var routingResponse = OsmRouting.GetRoute(currentLocation, destination, session);
                waypoints = routingResponse.Coordinates;
                var nextPath = routingResponse.Coordinates.Select(item => Tuple.Create(item.Latitude, item.Longitude)).ToList();
                session.EventDispatcher.Send(new NextRouteEvent
                {
                    Coords = nextPath
                });

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
                foreach (GeoCoordinate t in waypointsArr)
                {
                    // skip waypoints under 5 meters
                    var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                    double distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation,
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
                    return result;
                Navigation navi = new Navigation(_client, UpdatePositionEvent);
                var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
                if (LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)
                {
                    var nextSpeed = session.Client.rnd.NextInRange(walkingSpeedMin, walkingSpeedMax) * session.Settings.MoveSpeedFactor;

                    result = await navi.HumanPathWalking(destination, nextSpeed,
                        functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                }
            }
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
