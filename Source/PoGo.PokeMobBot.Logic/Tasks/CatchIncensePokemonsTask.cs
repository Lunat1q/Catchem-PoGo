﻿#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchIncensePokemonsTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!session.LogicSettings.CatchWildPokemon) return;
            if (session.Runtime.PokeBallsToCollect > 0) return;

            var usedItems = await session.Inventory.GetUsedItems();
            if (usedItems == null || !usedItems.Any(x=>x.ItemId == ItemId.ItemIncenseOrdinary || x.ItemId == ItemId.ItemIncenseSpicy ||
            x.ItemId == ItemId.ItemIncenseCool || x.ItemId == ItemId.ItemIncenseFloral)) return;

            // Refresh inventory so that the player stats are fresh
            //await session.Inventory.RefreshCachedInventory();
            
            session.EventDispatcher.Send(new DebugEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.LookingForIncensePokemon)
            });

            var incensePokemon = await session.Client.Map.GetIncensePokemons();
            if (incensePokemon.Result == GetIncensePokemonResponse.Types.Result.IncenseEncounterAvailable)
            {
                if (session.LogicSettings.UsePokemonToNotCatchFilter &&
                    session.LogicSettings.PokemonsNotToCatch.Contains(incensePokemon.PokemonId))
                {
                    //session.EventDispatcher.Send(new NoticeEvent
                    //{
                    //    Message = session.Translation.GetTranslation(TranslationString.PokemonIgnoreFilter, session.Translation.GetPokemonName(pokemon.PokemonId))
                    //});
                }
                else
                {
                    var mapPokemon = new MapPokemon
                    {
                        EncounterId = incensePokemon.EncounterId,
                        ExpirationTimestampMs = incensePokemon.DisappearTimestampMs,
                        Latitude = incensePokemon.Latitude,
                        Longitude = incensePokemon.Longitude,
                        PokemonId = incensePokemon.PokemonId,
                        SpawnPointId = incensePokemon.EncounterLocation
                    };
                    var pokemon = new PokemonCacheItem(mapPokemon);

                    session.EventDispatcher.Send(new PokemonsFoundEvent { Pokemons = new[] { mapPokemon } });

                    await Task.Delay(session.LogicSettings.DelayCatchIncensePokemon, cancellationToken);

                    var encounter =
                        await
                            session.Client.Encounter.EncounterIncensePokemon(pokemon.EncounterId,
                                pokemon.SpawnPointId);

                    if (encounter.Result == IncenseEncounterResponse.Types.Result.IncenseEncounterSuccess)
                    {
                        var catchRes = await CatchPokemonTask.Execute(session, encounter, pokemon, cancellationToken);
                        if (!catchRes)
                        {
                            session.Runtime.PokeBallsToCollect = 10;
                            return;
                        }
                    }
                    else if (encounter.Result == IncenseEncounterResponse.Types.Result.PokemonInventoryFull)
                    {
                        if (session.LogicSettings.TransferDuplicatePokemon)
                        {
                            session.EventDispatcher.Send(new WarnEvent
                            {
                                Message = session.Translation.GetTranslation(TranslationString.InvFullTransferring)
                            });
                            await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                        }
                        else
                            session.EventDispatcher.Send(new WarnEvent
                            {
                                Message = session.Translation.GetTranslation(TranslationString.InvFullTransferManually)
                            });
                    }
                    else
                    {
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.EncounterProblem, encounter.Result)
                        });
                    }
                    session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = pokemon.EncounterId });
                }
            }
        }
    }
}