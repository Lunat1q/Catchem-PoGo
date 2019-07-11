﻿#region using

using System;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;

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
                if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;
                // Refresh inventory so that the player stats are fresh
                await session.Inventory.RefreshCachedInventory();


                var all = await session.Inventory.GetPokemons();
                var pokemon = all.FirstOrDefault(p => p.Id == pokemonId);
                if (pokemon == null)
                {
                    session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                    return;
                }

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

                pokemon.Favorite = pokemon.Favorite == 0 ? 1 : 0;

                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
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
