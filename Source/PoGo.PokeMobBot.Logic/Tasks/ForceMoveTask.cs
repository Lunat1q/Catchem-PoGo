#region using directives

using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.GUI;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class ForceMoveTask
    {
        public static int TimesZeroXPawarded;

        public static async Task<PlayerUpdateResponse> Execute(ISession session, CancellationToken cancellationToken,
            bool silent = false)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (session.LogicSettings.UseEggIncubators && !session.EggWalker.Inited)
            {
                await session.EggWalker.InitEggWalker(cancellationToken);
            }

            var moveToCoords = session.ForceMoveTo;
            var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                session.Client.CurrentLongitude, moveToCoords.Latitude, moveToCoords.Longitude);

            if (!silent)
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message =
                        session.Translation.GetTranslation(TranslationString.ForceMove, session.ForceMoveTo.Latitude,
                            session.ForceMoveTo.Longitude, distance.ToString("N1"))
                });

            PlayerUpdateResponse result;
            session.ForceMoveToResume = session.ForceMoveTo;
            session.ForceMoveTo = null;
            session.State = BotState.Walk;
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
                        await UseNearbyPokestopsTask.Execute(session, cancellationToken, true);
                        return true;

                    }, cancellationToken, session);

            if (!silent)
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.ForceMoveDone)
                });
            session.State = BotState.Idle;
            session.ForceMoveJustDone = true;
            session.ForceMoveTo = null;
            session.ForceMoveToResume = null;
            session.EventDispatcher.Send(new ForceMoveDoneEvent());

            if (session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayPokestop, cancellationToken);
            else
                await Task.Delay(1000, cancellationToken);

            if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
            {
                await SnipePokemonTask.Execute(session, cancellationToken);
            }
            return result;
        }
    }
}