#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class FarmPokestopsCustomRouteTask
    {

        private static double NextMoveSpeed(ISession session)
        {
            return
                session.Client.rnd.NextInRange(session.LogicSettings.WalkingSpeedMin,
                    session.LogicSettings.WalkingSpeedMax)*session.Settings.MoveSpeedFactor;
        }

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var route = session.LogicSettings.CustomRoute;
            var eggWalker = new EggWalker(1000, session);

            if (route == null || route.RoutePoints.Count < 2)
            {
                session.EventDispatcher.Send(new BotCompleteFailureEvent()
                {
                   Shutdown = false,
                   Stop = true
                });
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "No proper route loaded, or route is too short"
                });
                return;
            }

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = $"You are using a custom route named: '{session.LogicSettings.CustomRouteName}' with {session.LogicSettings.CustomRoute.RoutePoints.Count} routing points"
            });

            var navi = new Navigation(session.Client);
            navi.UpdatePositionEvent += (lat, lng, alt) =>
            {
                session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng, Altitude = alt});
            };


            //PreLoad all pokestops which will be hitted during the route - can miss some (prolly)

            var allPokestopsInArea = await GetPokeStops(session);

            allPokestopsInArea =
                allPokestopsInArea.Where(
                    x =>
                        route.RoutePoints.Any(
                            v =>
                                LocationUtils.CalculateDistanceInMeters(x.Latitude, x.Longitude, v.Latitude, v.Longitude) <
                                40)).ToList();

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = allPokestopsInArea.Select(x => x.BaseFortData) });
            
            while (!cancellationToken.IsCancellationRequested)
            {
                await FollowTheYellowbrickroad(session, cancellationToken, route, navi, eggWalker, session.LogicSettings.CustomRouteName);
                route = session.LogicSettings.CustomRoute;
            }
        }

        private static async Task FollowTheYellowbrickroad(ISession session, CancellationToken cancellationToken, CustomRoute route,
            Navigation navi, EggWalker eggWalker, string prevRouteName)
        {
            var initialize = true;
            //Find closest point of route and it's index!
            var closestPoint = await CheckClosestAndMove(session, cancellationToken, route);
            long nextMaintenceStamp = 0;
            var sameRoute = true;
            while (sameRoute)
            {
                foreach (var wp in route.RoutePoints)
                {
                    if (session.ForceMoveTo != null)
                        break;

                    if (initialize)
                    {
                        if (wp != closestPoint) continue;
                        initialize = false;
                    }
                    if (prevRouteName != session.LogicSettings.CustomRouteName)
                    {
                        sameRoute = false;
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = $"Route switched from {prevRouteName} to {session.LogicSettings.CustomRouteName}!"
                        });
                        break;
                    }

                    session.State = BotState.Walk;

                    var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude, wp.Latitude, wp.Longitude);

                    await navi.HumanPathWalking(
                        session,
                        wp,
                        NextMoveSpeed(session),
                        async () =>
                        {
                            await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                            //Catch Incense Pokemon
                            await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                            return true;
                        },
                        async () =>
                        {
                            await UseNearbyPokestopsTask.Execute(session, cancellationToken, true);
                            await PokeNearbyGym.Execute(session, cancellationToken);
                            return true;
                        },
                        cancellationToken
                        );
                    session.State = BotState.Idle;
                    await eggWalker.ApplyDistance(distance, cancellationToken);
                    if (nextMaintenceStamp >= DateTime.UtcNow.ToUnixTime() && session.Runtime.StopsHit < 100) continue;
                    await MaintenanceTask.Execute(session, cancellationToken);
                    nextMaintenceStamp = DateTime.UtcNow.AddMinutes(3).ToUnixTime();
                }
                if (initialize)
                    initialize = false;

                if (session.ForceMoveTo != null)
                {
                    await ForceMoveTask.Execute(session, cancellationToken);
                    closestPoint = await CheckClosestAndMove(session, cancellationToken, route);
                    initialize = true;
                }
            }
        }

        private static async Task<GeoCoordinate> CheckClosestAndMove(ISession session, CancellationToken cancellationToken, CustomRoute route)
        {
            var closestPoint =
                route.RoutePoints.OrderBy(
                    x =>
                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                            session.Client.CurrentLongitude,
                            x.Latitude, x.Longitude)).First();
            var distToClosest = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                session.Client.CurrentLongitude,
                closestPoint.Latitude, closestPoint.Longitude);
            if (distToClosest > 10)
            {
                session.State = BotState.Walk;
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message =
                        $"Found closest point at {closestPoint.Latitude} - {closestPoint.Longitude}, distance to that point: {distToClosest.ToString("N1")} meters, moving there!"
                });
                await session.Navigation.Move(closestPoint,
                    session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                    async () =>
                    {
                        if (session.LogicSettings.CatchWildPokemon)
                        {
                            // Catch normal map Pokemon
                            await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                            //Catch Incense Pokemon
                            await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                        }
                        return true;
                    },
                    async () =>
                    {
                        await UseNearbyPokestopsTask.Execute(session, cancellationToken, true);
                        return true;
                    }, cancellationToken, session);
                session.State = BotState.Idle;
            }

            var nextPath = route.RoutePoints.Select(item => Tuple.Create(item.Latitude, item.Longitude)).ToList();
            session.EventDispatcher.Send(new NextRouteEvent
            {
                Coords = nextPath
            });

            return closestPoint;
        }

        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);

            //session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                pokeStops = pokeStops.Where(
                    i =>
                        i.Used == false && i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                            LocationUtils.CalculateDistanceInMeters(
                                session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                        session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }
            else
            {
                pokeStops = pokeStops.Where(
                        i =>
                            i.Used == false && i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }

            return pokeStops;
        }
    }
}