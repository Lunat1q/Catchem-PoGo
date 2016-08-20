#region using

using System;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;

#endregion
namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class FavoriteSpecificPokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken, ulong pokemonId)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Refresh inventory so that the player stats are fresh
                await session.Inventory.RefreshCachedInventory();


                var all = await session.Inventory.GetPokemons();
                var pokemon = all.FirstOrDefault(p => p.Id == pokemonId);
                if (pokemon == null) return;

                var pokemonSettings = await session.Inventory.GetPokemonSettings();
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                await session.Inventory.SetFavoritePokemon(pokemon.Id, pokemon.Favorite == 0);
                await Task.Delay(session.LogicSettings.DelayTransferPokemon, cancellationToken);
                session.EventDispatcher.Send(new PokemonFavoriteEvent
                {
                    Uid = pokemon.Id,
                    Pokemon = pokemon.PokemonId,
                    Cp = pokemon.Cp,
                    Iv = pokemon.CalculatePokemonPerfection(),
                    Candies = family.Candy_,
                    Favoured = pokemon.Favorite == 0
                });
            }
            catch (Exception)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.OperationCanceled)
                });
            }
        }
    }
}
