#region using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using GeoCoordinatePortable;

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
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "No proper route loaded"
                });
                return;
            }

            var navi = new Navigation(session.Client);
            navi.UpdatePositionEvent += (lat, lng, alt) =>
            {
                session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng, Altitude = alt});
            };
            if (LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                route.RoutePoints[0].Latitude, route.RoutePoints[0].Longitude) > 10)
            {
                session.State = BotState.Walk;
                await session.Navigation.Move(route.RoutePoints[0],
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

            long nextMaintenceStamp = 0;

            while (!cancellationToken.IsCancellationRequested)
            {

                foreach (var wp in route.RoutePoints)
                {

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
                            return true;
                        },
                        cancellationToken
                        );
                    session.State = BotState.Idle;
                    await eggWalker.ApplyDistance(distance, cancellationToken);
                    if (nextMaintenceStamp >= DateTime.UtcNow.ToUnixTime()) continue;
                    await MaintenanceTask.Execute(session, cancellationToken);
                    nextMaintenceStamp = DateTime.UtcNow.AddMinutes(3).ToUnixTime();
                }
            }
        }
    }
}