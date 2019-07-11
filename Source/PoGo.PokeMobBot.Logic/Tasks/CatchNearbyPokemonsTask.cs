#region using directives

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using System.Collections.Generic;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PokemonGo.RocketAPI.Extensions;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchNearbyPokemonsTask
    {
        public static async Task  Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //Refresh inventory so that the player stats are fresh
            //await session.Inventory.RefreshCachedInventory(); too much inventory refresh

            await CheckChallengeDoneTask.Execute(session, cancellationToken);
            await CheckChallengeTask.Execute(session, cancellationToken);

            if (!session.LogicSettings.CatchWildPokemon) return;

            if (session.Runtime.PokeBallsToCollect > 0) return;

            //session.EventDispatcher.Send(new DebugEvent()
            //{
            //    Message = session.Translation.GetTranslation(TranslationString.LookingForPokemon)
            //});

            var pokemons = await GetNearbyPokemons(session);

            if (session.LogicSettings.UsePokemonToNotCatchFilter)
            {
                pokemons = pokemons.Where(x => !session.LogicSettings.PokemonsNotToCatch.Contains(x.PokemonId)).ToList();
            }

            session.EventDispatcher.Send(new PokemonsFoundEvent { Pokemons = pokemons.Select(x => x.BaseMapPokemon) });
            if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;

            var prevState = session.State;
            session.State = BotState.FoundPokemons;

            pokemons = pokemons.OrderByDescending(x => x.PokemonId.HowRare()).ToList();

            foreach (var pokemon in pokemons)
            {
                if (pokemon.ExpirationTimestampMs > DateTime.UtcNow.ToUnixTime()) continue;

                cancellationToken.ThrowIfCancellationRequested();

                var pokeBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPokeBall);
                var greatBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemGreatBall);
                var ultraBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemUltraBall);
                var masterBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMasterBall);

                if (pokeBallsCount + greatBallsCount + ultraBallsCount + masterBallsCount == 0)
                {
                    session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.ZeroPokeballInv)
                    });
                    session.State = prevState;
                    session.Runtime.PokeBallsToCollect = 10;
                    session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = pokemon.EncounterId });
                    continue;
                }

                if (session.LogicSettings.UsePokemonToNotCatchFilter &&
                    session.LogicSettings.PokemonsNotToCatch.Contains(pokemon.PokemonId))
                {
                    if (!pokemon.Caught)
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonSkipped,
                                    session.Translation.GetPokemonName(pokemon.PokemonId))
                        });
                    pokemon.Caught = true;
                    session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = pokemon.EncounterId });
                    continue;
                }

                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, pokemon.Latitude, pokemon.Longitude);
                await Task.Delay(distance > 100 ? 3000 : 500, cancellationToken);

                var encounter =
                    await session.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId);

                try
                {
                    switch (encounter.Status)
                    {
                        case EncounterResponse.Types.Status.EncounterSuccess:
                            var catchRes = await CatchPokemonTask.Execute(session, encounter, pokemon, cancellationToken);
                            if (!catchRes)
                            {
                                session.Runtime.PokeBallsToCollect = 10;
                                session.State = prevState;

                                foreach (var p in pokemons)
                                    session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = p.EncounterId });

                                return;
                            }
                            break;
                        case EncounterResponse.Types.Status.PokemonInventoryFull:
                            if (session.LogicSettings.TransferDuplicatePokemon)
                            {
                                session.EventDispatcher.Send(new WarnEvent
                                {
                                    Message = session.Translation.GetTranslation(TranslationString.InvFullTransferring)
                                });
                                session.State = BotState.Idle;
                                await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                                session.State = BotState.FoundPokemons;
                            }
                            else
                                session.EventDispatcher.Send(new WarnEvent
                                {
                                    Message =
                                        session.Translation.GetTranslation(TranslationString.InvFullTransferManually)
                                });
                            break;
                        default:
                            session.EventDispatcher.Send(new WarnEvent
                            {
                                Message =
                                    session.Translation.GetTranslation(TranslationString.EncounterProblem,
                                        encounter.Status)
                            });
                            break;
                    }
                }
                catch (Exception)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.ErrorCatchNearby)
                    });
                    await Task.Delay(5000, cancellationToken);
                }

                session.EventDispatcher.Send(new PokemonDisappearEvent { EncounterId = pokemon.EncounterId });

                // always wait the delay amount between catches, ideally to prevent you from making another call too early after a catch event
                await Task.Delay(session.LogicSettings.DelayBetweenPokemonCatch, cancellationToken);
            }
            session.State = prevState;
        }

        private static async Task<List<PokemonCacheItem>> GetNearbyPokemons(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();
           
            var pokemons = await session.MapCache.MapPokemons(session);

            return pokemons;
        }
    }
}