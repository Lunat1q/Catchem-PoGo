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

                var pokemon = new MapPokemon
                {
                    EncounterId = encounterId,
                    Latitude = currentFortData.Latitude,
                    Longitude = currentFortData.Longitude,
                    PokemonId = pokemonId
                };
                session.EventDispatcher.Send(new PokemonsFoundEvent { Pokemons = new[] { pokemon } });

                try
                {
                    switch (encounter.Result)
                    {
                        case DiskEncounterResponse.Types.Result.Success:
                            await CatchPokemonTask.Execute(session, encounter, null, currentFortData, encounterId);
                            break;
                        case DiskEncounterResponse.Types.Result.PokemonInventoryFull:
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
                            break;
                        default:
                            if (encounter.Result.ToString().Contains("NotAvailable"))
                            {
                                session.EventDispatcher.Send(new PokemonDisappearEvent { Pokemon = pokemon });
                                return;
                            }
                            session.EventDispatcher.Send(new WarnEvent
                            {
                                Message =
                                    session.Translation.GetTranslation(TranslationString.EncounterProblemLurePokemon,
                                        encounter.Result)
                            });
                            break;
                    }
                }
                catch (Exception)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "Error occured while trying to catch lured pokemon"
                    });
                    await Task.Delay(5000, cancellationToken);
                }
                
                session.EventDispatcher.Send(new PokemonDisappearEvent { Pokemon = pokemon });
            }
        }
    }
}