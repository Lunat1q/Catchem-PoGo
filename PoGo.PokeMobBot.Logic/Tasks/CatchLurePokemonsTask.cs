#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using POGOProtos.Map.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchLurePokemonsTask
    {
        public static async Task Execute(ISession session, FortData currentFortData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();

            session.EventDispatcher.Send(new DebugEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.LookingForLurePokemon)
            });

            var fortId = currentFortData.Id;

            if (currentFortData.LureInfo == null) return;

            var pokemonId = currentFortData.LureInfo.ActivePokemonId;

            if (session.LogicSettings.UsePokemonToNotCatchFilter &&
                session.LogicSettings.PokemonsNotToCatch.Contains(pokemonId))
            {
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonSkipped, session.Translation.GetPokemonName(pokemonId))
                });
            }
            else
            {
                var encounterId = currentFortData.LureInfo.EncounterId;
                var encounter = await session.Client.Encounter.EncounterLurePokemon(encounterId, fortId);

                if (encounter.Result == DiskEncounterResponse.Types.Result.Success)
                {
                    //var pokemons = await session.MapCache.MapPokemons(session);
                    //var pokemon = pokemons.FirstOrDefault(i => i.PokemonId == encounter.PokemonData.PokemonId);
                    session.EventDispatcher.Send(new DebugEvent()
                    {
                        Message = "Found a Lure Pokemon."
                    });
                    
                    MapPokemon _pokemon = new MapPokemon
                    {
                        EncounterId = currentFortData.LureInfo.EncounterId,
                        ExpirationTimestampMs = currentFortData.LureInfo.LureExpiresTimestampMs,
                        Latitude = currentFortData.Latitude,
                        Longitude = currentFortData.Longitude,
                        PokemonId = currentFortData.LureInfo.ActivePokemonId,
                        SpawnPointId = currentFortData.LureInfo.FortId
                    };
                    if (session.LogicSettings.UsePokemonToNotCatchFilter &&
                        session.LogicSettings.PokemonsNotToCatch.Contains(_pokemon.PokemonId))
                    {
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = session.Translation.GetTranslation(TranslationString.PokemonIgnoreFilter, session.Translation.GetPokemonName(_pokemon.PokemonId))
                        });
                    }
                    else
                    {
                        session.EventDispatcher.Send(new PokemonsFoundEvent { Pokemons = new[] { _pokemon } });
                        PokemonCacheItem pokemon = new PokemonCacheItem(_pokemon);

                        await CatchPokemonTask.Execute(session, encounter, pokemon, cancellationToken, currentFortData, encounterId);
                        currentFortData.LureInfo = null;
                        session.EventDispatcher.Send(new PokemonDisappearEvent { Pokemon = _pokemon });
                    }
                    
                    //await CatchPokemonTask.Execute(session, encounter, pokemon, currentFortData, encounterId);
                }
                else if (encounter.Result == DiskEncounterResponse.Types.Result.PokemonInventoryFull)
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
                    if (encounter.Result.ToString().Contains("NotAvailable")) return;
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message =
                            session.Translation.GetTranslation(TranslationString.EncounterProblemLurePokemon,
                                encounter.Result)
                    });
                }
                // always wait the delay amount between catches, ideally to prevent you from making another call too early after a catch event
                await Task.Delay(session.LogicSettings.DelayBetweenPokemonCatch);
            }
        }
    }
}