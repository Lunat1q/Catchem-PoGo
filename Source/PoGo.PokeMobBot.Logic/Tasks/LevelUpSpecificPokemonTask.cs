#region using directives

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using POGOProtos.Networking.Responses;
using Logger = PoGo.PokeMobBot.Logic.Logging.Logger;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class LevelUpSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId, CancellationToken cancellationToken, bool toMax = false)
        {
            try
            {
                if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;
            }
            catch (TaskCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                Logger.Write($"[MANUAL TASAK FAIL] ERROR: {ex.Message}");
            }
            var prevState = session.State;
            session.State = BotState.LevelPoke;

            var all = await session.Inventory.GetPokemons();
            var pokemon = all.FirstOrDefault(p => p.Id == pokemonId);
            if (pokemon == null)
            {
                session.State = prevState;
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }


            if (!string.IsNullOrEmpty(pokemon.DeployedFortId))
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokeInGym, string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)
                });
                session.State = prevState;
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }


            if (pokemon.GetLevel() >= session.Runtime.CurrentLevel + 1.5)
            {
                session.State = prevState;
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            bool success;
            var max = false;
            UpgradePokemonResponse latestSuccessResponse = null;
            do
            {
                success = false;
                var upgradeResult = await session.Inventory.UpgradePokemon(pokemon.Id);
                await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
                switch (upgradeResult.Result)
                {
                    case UpgradePokemonResponse.Types.Result.Success:
                        success = true;
                        latestSuccessResponse = upgradeResult;
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeSuccess,
                                    session.Translation.GetPokemonName(upgradeResult.UpgradedPokemon.PokemonId),
                                    upgradeResult.UpgradedPokemon.Cp)
                        });
                        if (upgradeResult.UpgradedPokemon.GetLevel() >= session.Runtime.CurrentLevel + 1.5) max = true;
                        break;
                    case UpgradePokemonResponse.Types.Result.ErrorInsufficientResources:
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailed)
                        });
                        break;
                    case UpgradePokemonResponse.Types.Result.ErrorUpgradeNotAvailable:
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeUnavailable,
                                    session.Translation.GetPokemonName(pokemon.PokemonId), pokemon.Cp,
                                    PokemonInfo.CalculateMaxCp(pokemon))
                        });
                        break;
                    default:
                        session.EventDispatcher.Send(new NoticeEvent
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailedError,
                                    session.Translation.GetPokemonName(pokemon.PokemonId))
                        });
                        break;
                }
            } while (success && toMax && !cancellationToken.IsCancellationRequested && !max);

            if (latestSuccessResponse != null && !cancellationToken.IsCancellationRequested)
            {
                var mon = latestSuccessResponse.UpgradedPokemon;
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                session.EventDispatcher.Send(new PokemonStatsChangedEvent
                {
                    Name = !string.IsNullOrEmpty(mon.Nickname)
                        ? mon.Nickname
                        : session.Translation.GetPokemonName(mon.PokemonId),
                    Uid = pokemonId,
                    Id = mon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = mon.Cp,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(mon, session.Runtime.CurrentLevel),
                    Iv = mon.CalculatePokemonPerfection(),
                    Favourite = mon.Favorite == 1,
                    Weight = mon.WeightKg,
                    Cpm = mon.CpMultiplier + mon.AdditionalCpMultiplier,
                    Level = mon.GetLevel(),
                    IvDef = mon.IndividualDefense,
                    IvAtk = mon.IndividualAttack,
                    Stamina = mon.Stamina,
                    StaminaMax = mon.StaminaMax
                });
                session.EventDispatcher.Send(new PokemonActionDoneEvent
                {
                   Uid = pokemonId
                });
            }
            session.State = prevState;
        }
    }
}