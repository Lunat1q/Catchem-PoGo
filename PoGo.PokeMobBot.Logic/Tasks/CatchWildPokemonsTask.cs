#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Map.Pokemon;
using GeoCoordinatePortable;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchWildPokemonsTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!session.LogicSettings.CatchWildPokemon) return;

            Logger.Write(session.Translation.GetTranslation(TranslationString.LookingForPokemon), LogLevel.Debug, session: session);

            var pokemons = await GetWildPokemons(session);
            if (pokemons != null && pokemons.Any())
            {
                session.EventDispatcher.Send(new NoticeEvent{
                    Message = "Found some hiding pokemons in the area, trying to catch'em now!"
                });
                session.EventDispatcher.Send(new PokemonsWildFoundEvent {Pokemons = pokemons});
                foreach (var pokemon in pokemons)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (session.LogicSettings.Teleport)
                        await session.Client.Player.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude,
                            session.Client.Settings.DefaultAltitude);
                    else
                        await MoveToPokemon(pokemon, session, cancellationToken);
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
                await session.Navigation.Move(new GeoCoordinate(waypoint.Latitude, waypoint.Longitude),
                        session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                async () => 
                {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    return true;
                }, null, cancellationToken, session);
                if (session.MapCache.CheckPokemonCaught(pokemon.EncounterId)) return;
                waypoint = LocationUtils.CreateWaypoint(waypoint, nextWaypointDistance, nextWaypointBearing);
            }
            
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