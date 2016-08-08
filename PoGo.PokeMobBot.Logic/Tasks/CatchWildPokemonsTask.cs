#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using GeoCoordinatePortable;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchWildPokemonsTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Write(session.Translation.GetTranslation(TranslationString.LookingForPokemon), LogLevel.Debug, session: session);

            var pokemons = await GetWildPokemons(session);
            session.EventDispatcher.Send(new PokemonsWildFoundEvent { Pokemons = pokemons });
            foreach (var pokemon in pokemons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (session.LogicSettings.Teleport)
                    await session.Client.Player.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude,
                        session.Client.Settings.DefaultAltitude);
                else
                    await session.Navigation.HumanLikeWalking(new GeoCoordinate(pokemon.Latitude, pokemon.Longitude),
                    session.LogicSettings.WalkingSpeedInKilometerPerHour,
                    async () =>
                    {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                        return true;
                    }, cancellationToken);
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