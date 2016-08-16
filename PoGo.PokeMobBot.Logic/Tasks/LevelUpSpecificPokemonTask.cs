#region using directives

using System.Linq;
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
        public static async Task Execute(ISession session, ulong pokemonId, bool toMax = false)
        {
            var all = await session.Inventory.GetPokemons();
            var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
            var pokemon = pokemons.FirstOrDefault(p => p.Id == pokemonId);
            if (pokemon == null) return;
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
                session.EventDispatcher.Send(new PokemonStatsChangedEvent()
                {
                    Uid = pokemonId,
                    Id = pokemon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = latestSuccessResponse.UpgradedPokemon.Cp,
                    Iv = latestSuccessResponse.UpgradedPokemon.CalculatePokemonPerfection()
                });
            }
        }
    }
}