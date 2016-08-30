#region using directives

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Common;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class LevelUpSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId, CancellationToken cancellationToken, bool toMax = false)
        {

            if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;

            var prevState = session.State;
            session.State = BotState.LevelPoke;
            var all = await session.Inventory.GetPokemons();
            var pokemon = all.FirstOrDefault(p => p.Id == pokemonId);
            if (pokemon == null) return;

            if (!string.IsNullOrEmpty(pokemon.DeployedFortId))
            {
                session.EventDispatcher.Send(new WarnEvent()
                {
                    Message = $"Pokemon {(string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)} is signed to defend a GYM!"
                });
                return;
            }

            bool success;
            UpgradePokemonResponse latestSuccessResponse = null;
            do
            {
                success = false;
                var upgradeResult = await session.Inventory.UpgradePokemon(pokemon.Id);

                switch (upgradeResult.Result)
                {
                    case UpgradePokemonResponse.Types.Result.Success:
                        success = true;
                        latestSuccessResponse = upgradeResult;
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeSuccess,
                                    session.Translation.GetPokemonName(upgradeResult.UpgradedPokemon.PokemonId),
                                    upgradeResult.UpgradedPokemon.Cp)
                        });
                        break;
                    case UpgradePokemonResponse.Types.Result.ErrorInsufficientResources:
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailed)
                        });
                        break;
                    case UpgradePokemonResponse.Types.Result.ErrorUpgradeNotAvailable:
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeUnavailable,
                                    session.Translation.GetPokemonName(pokemon.PokemonId), pokemon.Cp,
                                    PokemonInfo.CalculateMaxCp(pokemon))
                        });
                        break;
                    default:
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message =
                                session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailedError,
                                    session.Translation.GetPokemonName(pokemon.PokemonId))
                        });
                        break;
                }
                await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
            } while (success && toMax);

            if (latestSuccessResponse != null)
            {
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                session.EventDispatcher.Send(new PokemonStatsChangedEvent
                {
                    Name = !string.IsNullOrEmpty(pokemon.Nickname)
                        ? pokemon.Nickname
                        : session.Translation.GetPokemonName(pokemon.PokemonId),
                    Uid = pokemonId,
                    Id = pokemon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = latestSuccessResponse.UpgradedPokemon.Cp,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(latestSuccessResponse.UpgradedPokemon, session.Runtime.CurrentLevel),
                    Iv = latestSuccessResponse.UpgradedPokemon.CalculatePokemonPerfection(),
                    Favourite = pokemon.Favorite == 1
                });
            }
            session.State = prevState;
        }
    }
}