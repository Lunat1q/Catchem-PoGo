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
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI.Extensions;
using RandomExtensions = PoGo.PokeMobBot.Logic.Extensions.RandomExtensions;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination">Desired location to move</param>
        /// <param name="walkingSpeedMin">Minimal walking speed during the move</param>
        /// <param name="walkingSpeedMax">Maximal walking speed during the move</param>
        /// <param name="functionExecutedWhileWalking">Functions #1 to be exec while walking, like task or smth</param>
        /// <param name="functionExecutedWhileWalking2">Functions #1 to be exec while walking, like task or smth</param>
        /// <param name="cancellationToken">regular session cancelation token</param>
        /// <param name="session">ISession param of the bot, to detect which bot started it</param>
        /// <param name="direct">Directly move to the point, skip routing services</param>
        /// <param name="waypointsToVisit">Waypoints to visit during the move, required to redure Google Directions API usage</param>
        /// <param name="eggWalker"></param>
        /// <returns></returns>
        internal async Task<PlayerUpdateResponse> Move(GeoCoordinate destination, double walkingSpeedMin, double walkingSpeedMax, Func<Task<bool>> functionExecutedWhileWalking, Func<Task<bool>> functionExecutedWhileWalking2,
            CancellationToken cancellationToken, ISession session, bool direct = false, List<GeoCoordinate> waypointsToVisit = null, EggWalker eggWalker = null )
        {
            var currentLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude, _client.CurrentAltitude);
            var result = new PlayerUpdateResponse();
            if (session.LogicSettings.UseHumanPathing)
            {

                var waypoints = new List<GeoCoordinate>();       

                if (!direct)
                {
					RoutingResponse routingResponse = null;
	                try
	                {
	                    switch (session.LogicSettings.RoutingService)
	                    {
	                        case RoutingService.MobBot:
	                            routingResponse = Routing.GetRoute(currentLocation, destination, session);
                                break;
	                        case RoutingService.OpenLs:
                                routingResponse = OsmRouting.GetRoute(currentLocation, destination, session);
                                break;
	                        case RoutingService.GoogleDirections:
	                            routingResponse = GoogleRouting.GetRoute(currentLocation, destination, session,
	                                waypointsToVisit);
	                            break;
	                        case RoutingService.MapzenValhalla:
                                routingResponse = MapzenRouting.GetRoute(currentLocation, destination, session,
                                    waypointsToVisit);
                                break;
	                    }
	                }
	                catch (NullReferenceException ex)
	                {
	                    session.EventDispatcher.Send(new DebugEvent
	                    {
	                        Message = ex.ToString()
	                    });
	                    routingResponse = new RoutingResponse();
	                }

                    if (routingResponse?.Coordinates != null)
                    {
                        foreach (var item in routingResponse.Coordinates)
                        {
                            if (item == null) continue;
                            //0 = lat, 1 = long (MAYBE NOT THO?)
                            switch (session.LogicSettings.RoutingService)
                            {
                                case RoutingService.MobBot:
                                    waypoints.Add(new GeoCoordinate(item[1], item[0]));
                                    break;
                                case RoutingService.OpenLs:
                                    waypoints.Add(new GeoCoordinate(item.ToArray()[1], item.ToArray()[0], item.ToArray()[2]));
                                    break;
                                case RoutingService.GoogleDirections:
                                    waypoints.Add(new GeoCoordinate(item[0], item[1]));
                                    break;
                                case RoutingService.MapzenValhalla:
                                    waypoints.Add(new GeoCoordinate(item[0], item[1]));
                                    break;
                            }
                        }
                        if ((session.LogicSettings.RoutingService == RoutingService.GoogleDirections || session.LogicSettings.RoutingService == RoutingService.MobBot || session.LogicSettings.RoutingService == RoutingService.MapzenValhalla) && session.LogicSettings.UseMapzenApiElevation)
                        {
                            waypoints = await session.MapzenApi.FillAltitude(waypoints);
                        }
                    }
                }

                if (waypoints.Count == 0)
                    waypoints.Add(destination);
                else if (waypoints.Count > 1)
                {
                    var nextPath = waypoints.Select(item => Tuple.Create(item.Latitude, item.Longitude)).ToList();
                    session.EventDispatcher.Send(new NextRouteEvent
                    {
                        Coords = nextPath
                    });
                    destination = waypoints.Last();
                }

                var navi = new Navigation(_client, UpdatePositionEvent);
                var waypointsArr = waypoints.ToArray();
                long nextMaintenceStamp = 0;
                //MILD REWRITE TO USE HUMANPATHWALKING;
                foreach (var t in waypointsArr)
                {
                    if (session.ForceMoveTo != null)
                    {
                        return await ForceMoveTask.Execute(session, cancellationToken);
                    }
                    // skip waypoints under 5 meters
                    var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                    var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, t);
                    if (distanceToTarget <= 5)
                        continue;

                    var nextSpeed = RandomExtensions.NextInRange(session.Client.rnd, walkingSpeedMin, walkingSpeedMax)*session.Settings.MoveSpeedFactor;
                    session.State = BotState.Walk;
                    result = await navi.HumanPathWalking(session, t, nextSpeed, functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                    if (session.Runtime.BreakOutOfPathing)
                        return result;
                    UpdatePositionEvent?.Invoke(t.Latitude, t.Longitude, t.Altitude);

                    if (nextMaintenceStamp < DateTime.UtcNow.ToUnixTime())
                    {
                        await MaintenanceTask.Execute(session, cancellationToken);
                        nextMaintenceStamp = DateTime.UtcNow.AddMinutes(3).ToUnixTime();
                    }
                    if (eggWalker != null)
                        await eggWalker.ApplyDistance(distanceToTarget, cancellationToken);
                }
                session.State = BotState.Idle;
                var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
                if (LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)
                {
                    var nextSpeed = RandomExtensions.NextInRange(session.Client.rnd, walkingSpeedMin, walkingSpeedMax)*session.Settings.MoveSpeedFactor;

                    result = await navi.HumanPathWalking(session, destination, nextSpeed, functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                }
                await MaintenanceTask.Execute(session, cancellationToken);
            }
            else
            {
                if (destination.Latitude.Equals(session.Runtime.lastPokeStopCoordinate.Latitude) && destination.Longitude.Equals(session.Runtime.lastPokeStopCoordinate.Longitude))
                    session.Runtime.BreakOutOfPathing = true;

                if (session.Runtime.BreakOutOfPathing)
                {
                    await MaintenanceTask.Execute(session, cancellationToken);
                    return result;
                }
                var navi = new Navigation(_client, UpdatePositionEvent);
                var curcoord = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
                if (LocationUtils.CalculateDistanceInMeters(curcoord, destination) > 40)
                {
                    var nextSpeed = RandomExtensions.NextInRange(session.Client.rnd, walkingSpeedMin, walkingSpeedMax)*session.Settings.MoveSpeedFactor;

                    result = await navi.HumanPathWalking(session, destination, nextSpeed, functionExecutedWhileWalking, functionExecutedWhileWalking2, cancellationToken);
                }
            }
            session.State = BotState.Idle;
            await MaintenanceTask.Execute(session, cancellationToken);
            return result;
        }

        public static double GetAccelerationTime(double curV, double maxV, double acc)
        {
            if (Math.Abs(acc) < 0.001)
                return 9001;
            else
                return (maxV - curV)/acc;
        }

        public static double GetDistanceTraveledAccelerating(double time, double acc, double curV)
        {
            return ((curV*time) + ((acc*Math.Pow(time, 2))/2));
        }

        public event UpdatePositionDelegate UpdatePositionEvent;
    }
}
