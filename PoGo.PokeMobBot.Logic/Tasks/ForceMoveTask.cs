#region using directives

using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class ForceMoveTask
    {
        public static int TimesZeroXPawarded;

        public static async Task<PlayerUpdateResponse> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eggWalker = new EggWalker(1000, session);

           
            var moveToCoords = session.ForceMoveTo;
            var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                session.Client.CurrentLongitude, moveToCoords.Latitude, moveToCoords.Longitude);

            session.EventDispatcher.Send(new WarnEvent
            {
                Message = $"ForceMove to {session.ForceMoveTo.Latitude} - {session.ForceMoveTo.Longitude} Started! Distance: {distance.ToString("N1")}m"
            });
            session.ForceMoveTo = null;
            PlayerUpdateResponse result;

            if (session.LogicSettings.Teleport)
                result = await session.Client.Player.UpdatePlayerLocation(moveToCoords.Latitude, moveToCoords.Longitude,
                    session.Client.Settings.DefaultAltitude);
            else
                result = await session.Navigation.Move(new GeoCoordinate(moveToCoords.Latitude, moveToCoords.Longitude),
                session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                async () =>
                {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    return true;
                }, 
                async () =>
                {
                    await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                    return true;

                }, cancellationToken, session);

            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "ForceMove Done!"
            });
            session.ForceMoveJustDone = true;
            session.ForceMoveTo = null;
            session.EventDispatcher.Send(new ForceMoveDoneEvent() );

            if (session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayPokestop, cancellationToken);
            else
                await Task.Delay(1000, cancellationToken);

            await eggWalker.ApplyDistance(distance, cancellationToken);


            if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
            {
                await SnipePokemonTask.Execute(session, cancellationToken);
            }
            return result;
        }
    }
}