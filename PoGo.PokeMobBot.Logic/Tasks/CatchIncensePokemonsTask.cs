﻿#region using directives

using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Map.Pokemon;
using POGOProtos.Inventory;
using POGOProtos.Networking.Responses;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchIncensePokemonsTask
    {
        private static readonly DateTime Jan1st1970 = new DateTime
                (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            bool catchIncenseActive = new bool();
            var currentActiveItems = await session.Inventory.GetUsedItems();
            currentActiveItems.ForEach(delegate (AppliedItem singleItem)
            {
                if (singleItem.ItemType == ItemType.Incense)
                {
                    var _expireMs = singleItem.ExpireMs;
                    var _appliedMs = singleItem.AppliedMs;
                    var currentMillis = CurrentTimeMillis();
                    if (currentMillis < (_expireMs + 30000)) //+30 seconds to catch the last incense mons
                    {
                        catchIncenseActive = true;
                    }
                    else
                    {
                        catchIncenseActive = false;
                    }
                    
                }
            });
            if (catchIncenseActive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!session.LogicSettings.CatchWildPokemon) return;
                if (session.Runtime.PokeBallsToCollect > 0) return;
                session.EventDispatcher.Send(new DebugEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.LookingForIncensePokemon)
                });

                var incensePokemon = await session.Client.Map.GetIncensePokemons();
                if (incensePokemon.Result == GetIncensePokemonResponse.Types.Result.IncenseEncounterAvailable)
                {
                    var _pokemon = new MapPokemon
                    {
                        EncounterId = incensePokemon.EncounterId,
                        ExpirationTimestampMs = incensePokemon.DisappearTimestampMs,
                        Latitude = incensePokemon.Latitude,
                        Longitude = incensePokemon.Longitude,
                        PokemonId = incensePokemon.PokemonId,
                        SpawnPointId = incensePokemon.EncounterLocation
                    };
                    var pokemon = new PokemonCacheItem(_pokemon);
                    if (session.LogicSettings.UsePokemonToNotCatchFilter &&
                        session.LogicSettings.PokemonsNotToCatch.Contains(pokemon.PokemonId))
                    {
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = session.Translation.GetTranslation(TranslationString.PokemonIgnoreFilter, session.Translation.GetPokemonName(pokemon.PokemonId))
                        });
                    }
                    else
                    {
                        session.EventDispatcher.Send(new PokemonsFoundEvent { Pokemons = new[] { _pokemon } });

                        await Task.Delay(session.LogicSettings.DelayCatchIncensePokemon);

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
                    }
                    session.EventDispatcher.Send(new PokemonDisappearEvent { Pokemon = pokemon.BaseMapPokemon });
                }
                else
                {
                    //Cancel that freaking spamming task
                }
            }
            
        }
    }
}