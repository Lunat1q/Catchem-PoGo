#region using
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#endregion
namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class FavoritePokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();


            var pokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
            var pokemons = await session.Inventory.GetPokemons();
            //pokemons not in gym, not favorited, and IV above FavoriteMinIv %
            var pokemonsToBeFavorited = pokemons.Where(p => p.DeployedFortId == string.Empty &&
                        p.Favorite == 0 && (PokemonInfo.CalculatePokemonPerfection(p) > session.LogicSettings.FavoriteMinIvPercentage)).ToList();
            //favorite
            foreach (var pokemon in pokemonsToBeFavorited)
            {
                if (pokemon.Favorite == 0)
                {
                    var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                    var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

                    await session.Inventory.SetFavoritePokemon(pokemon.Id, true);
                    session.EventDispatcher.Send(new PokemonFavoriteEvent
                    {
                        Uid = pokemon.Id,
                        Pokemon = pokemon.PokemonId,
                        Cp = pokemon.Cp,
                        Iv = pokemon.CalculatePokemonPerfection(),
                        Candies = family.Candy_,
                        Favoured = true
                    });
                }
                await Task.Delay(session.LogicSettings.DelayTransferPokemon, cancellationToken);
            }
            //pokemons not in gym, favorited, and IV lower than FavoriteMinIv %
            var pokemonsToBeUnFavorited = pokemons.Where(p => p.DeployedFortId == string.Empty &&
                        p.Favorite == 1 && (p.CalculatePokemonPerfection() < session.LogicSettings.FavoriteMinIvPercentage)).ToList();
            //unfavorite
            foreach (var pokemon in pokemonsToBeUnFavorited)
            {
                if (pokemon.Favorite == 1)
                {
                    var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                    var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

                    await session.Inventory.SetFavoritePokemon(pokemon.Id, false);
                    session.EventDispatcher.Send(new PokemonFavoriteEvent
                    {
                        Uid = pokemon.Id,
                        Pokemon = pokemon.PokemonId,
                        Cp = pokemon.Cp,
                        Iv = pokemon.CalculatePokemonPerfection(),
                        Candies = family.Candy_,
                        Favoured = false
                    });
                }
                await Task.Delay(session.LogicSettings.DelayTransferPokemon, cancellationToken);
            }
        }
    }
}
