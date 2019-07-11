#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Map.Pokemon;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchWildPokemonsTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!session.LogicSettings.CatchWildPokemon) return;
            if (session.Runtime.PokeBallsToCollect > 0) return;

            Logger.Write(session.Translation.GetTranslation(TranslationString.LookingForPokemon), LogLevel.Debug, session: session);

            var pokemons = await GetWildPokemons(session);

            pokemons =
                    pokemons?.Where(x => session.LogicSettings.PokemonsNotToCatch.All(v => v != x.PokemonData?.PokemonId))
                        .OrderByDescending(x => x.PokemonData?.PokemonId.HowRare());

            if (pokemons != null && pokemons.Any())
            {
                var hiddenPokeNames =
                    pokemons.Select(
                        x =>
                            x.PokemonData?.Id != null ? session.Translation.GetPokemonName(x.PokemonData.PokemonId) : "")
                        .Aggregate((x, v) => x + ", " + v);
                session.EventDispatcher.Send(new NoticeEvent{
                    Message = session.Translation.GetTranslation(TranslationString.FoundHiding) + " - " + hiddenPokeNames
                });
                session.EventDispatcher.Send(new PokemonsWildFoundEvent {Pokemons = pokemons});
                foreach (var pokemon in pokemons)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;

                    if (session.LogicSettings.Teleport)
                        await session.Client.Player.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude,
                            session.Client.Settings.DefaultAltitude);
                    else
                        await MoveToPokemon(pokemon, session, cancellationToken);
                    session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = pokemon.EncounterId });
                    if (!session.ForceMoveJustDone) continue;
                    foreach (var p in pokemons)
                    {
                        session.EventDispatcher.Send(new PokemonDisappearEvent {EncounterId = p.EncounterId});
                    }
                    break;
                }
            }
        }

        private static async Task MoveToPokemon(WildPokemon pokemon, ISession session, CancellationToken cancellationToken)
        {
            //split the way in 5 steps
            var sourceLocation = new GeoCoordinate(session.Client.CurrentLatitude, session.Client.CurrentLongitude);
            var targetLocation = new GeoCoordinate(pokemon.Latitude, pokemon.Longitude);
            var distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, new GeoCoordinate(pokemon.Latitude, pokemon.Longitude));
            var nextWaypointDistance = distanceToTarget/5;
            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);
            for (var i = 0; i < 5; i++)
            {
                if (session.MapCache.CheckPokemonCaught(pokemon.EncounterId) || session.ForceMoveJustDone) break;
                await MoveTo(waypoint, session, cancellationToken);
                waypoint = LocationUtils.CreateWaypoint(waypoint, nextWaypointDistance, nextWaypointBearing);
            }
            if (!session.ForceMoveJustDone)
                await MoveTo(sourceLocation, session, cancellationToken);
        }

        private static async Task MoveTo(GeoCoordinate waypoint, ISession session, CancellationToken cancellationToken)
        {
            await session.Navigation.Move(new GeoCoordinate(waypoint.Latitude, waypoint.Longitude),
                        session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                async () =>
                {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    return true;
                }, null, cancellationToken, session, sendRoute: false);
        }

        private static async Task<IOrderedEnumerable<WildPokemon>> GetWildPokemons(ISession session)
        {
            var mapObjects = await session.Client.Map.GetMapObjects();

            var pokemons = mapObjects.MapCells.SelectMany(i => i.WildPokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                            session.Client.CurrentLongitude,
                            i.Latitude, i.Longitude));

            return pokemons;
        }
    }
}