#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class ForceMoveTask
    {
        public static int TimesZeroXPawarded;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eggWalker = new EggWalker(1000, session);

            session.EventDispatcher.Send(new WarnEvent
            {
                Message = $"ForceMove to {session.ForceMoveTo.Latitude} - {session.ForceMoveTo.Longitude} Started!"
            });


            var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                session.Client.CurrentLongitude, session.ForceMoveTo.Latitude, session.ForceMoveTo.Longitude);

            if (session.LogicSettings.Teleport)
                await session.Client.Player.UpdatePlayerLocation(session.ForceMoveTo.Latitude, session.ForceMoveTo.Longitude,
                    session.Client.Settings.DefaultAltitude);
            else
                await session.Navigation.HumanLikeWalking(new GeoCoordinate(session.ForceMoveTo.Latitude, session.ForceMoveTo.Longitude),
                session.LogicSettings.WalkingSpeedInKilometerPerHour,
                async () =>
                {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    return true;
                }, cancellationToken);

            session.EventDispatcher.Send(new WarnEvent
            {
                Message = $"ForceMove Done!"
            });
            session.ForceMoveTo = null;
            session.EventDispatcher.Send(new ForceMoveDoneEvent() );

            if (session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayPokestop);
            else
                await Task.Delay(1000, cancellationToken);

            await eggWalker.ApplyDistance(distance, cancellationToken);


            if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
            {
                await SnipePokemonTask.Execute(session, cancellationToken);
            }
        }
    }
}