#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using POGOProtos.Inventory;
using POGOProtos.Settings.Master;
using POGOProtos.Data;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.PoGoUtils;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class LevelUpPokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();
            var prevState = session.State;
            session.State = BotState.LevelPoke;
            // get the families and the pokemons settings to do some actual smart stuff like checking if you have enough candy in the first place
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
            var pokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonUpgradeSettings = await session.Inventory.GetPokemonUpgradeSettings();
            var playerLevel = await session.Inventory.GetPlayerStats();

            List<PokemonData> allPokemon = new List<PokemonData>();

            // priority for upgrading
            if (session.LogicSettings.LevelUpByCPorIv?.ToLower() == "iv")
            {
                allPokemon = session.Inventory.GetHighestsPerfect(session.Profile.PlayerData.MaxPokemonStorage).Result.ToList();
            }
            else if (session.LogicSettings.LevelUpByCPorIv?.ToLower() == "cp")
            {
                allPokemon = session.Inventory.GetPokemons().Result.OrderByDescending(p => p.Cp).ToList();
            }

            // iterate on whatever meets both minimums
            // to disable one or the other, set to 0
            foreach (var pokemon in allPokemon.Where(p => string.IsNullOrEmpty(p.DeployedFortId) && session.Inventory.GetPerfect(p) >= session.LogicSettings.UpgradePokemonIvMinimum && p.Cp >= session.LogicSettings.UpgradePokemonCpMinimum))
            {
                int pokeLevel = (int)PokemonInfo.GetLevel(pokemon);
                var currentPokemonSettings = pokemonSettings.FirstOrDefault(q => pokemon != null && q.PokemonId.Equals(pokemon.PokemonId));
                var family = pokemonFamilies.FirstOrDefault(q => currentPokemonSettings != null && q.FamilyId.Equals(currentPokemonSettings.FamilyId));
                int candyToEvolveTotal = GetCandyMinToKeep(pokemonSettings, currentPokemonSettings);

                // you can upgrade up to player level+2 right now
                // may need translation for stardust???
                if (pokeLevel < playerLevel?.FirstOrDefault().Level + pokemonUpgradeSettings.FirstOrDefault().AllowedLevelsAbovePlayer
                    && family.Candy_ > pokemonUpgradeSettings.FirstOrDefault()?.CandyCost[pokeLevel]
                    && family.Candy_ >= candyToEvolveTotal
                    && session.Profile.PlayerData.Currencies.FirstOrDefault(c => c.Name.ToLower().Contains("stardust")).Amount >= pokemonUpgradeSettings.FirstOrDefault()?.StardustCost[pokeLevel])
                {
                    await DoUpgrade(session, pokemon);
                }
            }
            session.State = prevState;
        }

        private static int GetCandyMinToKeep(IEnumerable<PokemonSettings> pokemonSettings, PokemonSettings currentPokemonSettings)
        {
            // total up required candy for evolution, for yourself and your ancestors to allow for others to be evolved before upgrading
            // always keeps a minimum amount in reserve, should never have 0 except for cases where a pokemon is in both first and final form (ie onix)
            var ancestor = pokemonSettings.FirstOrDefault(q => q.PokemonId == currentPokemonSettings.ParentPokemonId);
            var ancestor2 = pokemonSettings.FirstOrDefault(q => q.PokemonId == ancestor?.ParentPokemonId);

            int candyToEvolveTotal = currentPokemonSettings.CandyToEvolve;
            if (ancestor != null)
            {
                candyToEvolveTotal += ancestor.CandyToEvolve;
            }

            if (ancestor2 != null)
            {
                candyToEvolveTotal += ancestor2.CandyToEvolve;
            }

            return candyToEvolveTotal;
        }

        private static async Task DoUpgrade(ISession session, PokemonData pokemon)
        {
            var upgradeResult = await session.Inventory.UpgradePokemon(pokemon.Id);

            if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.Success)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeSuccess, session.Translation.GetPokemonName(upgradeResult.UpgradedPokemon.PokemonId), upgradeResult.UpgradedPokemon.Cp)
                });
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                session.EventDispatcher.Send(new PokemonStatsChangedEvent
                {
                    Name = !string.IsNullOrEmpty(pokemon.Nickname)
                       ? pokemon.Nickname
                       : session.Translation.GetPokemonName(pokemon.PokemonId),
                    Uid = pokemon.Id,
                    Id = pokemon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = upgradeResult.UpgradedPokemon.Cp,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(upgradeResult.UpgradedPokemon, session.Runtime.CurrentLevel),
                    Iv = upgradeResult.UpgradedPokemon.CalculatePokemonPerfection(),
                    Favourite = pokemon.Favorite == 1
                });
            }
            else if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.ErrorInsufficientResources)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailed)
                });
            }
            // pokemon max level
            else if (upgradeResult.Result == POGOProtos.Networking.Responses.UpgradePokemonResponse.Types.Result.ErrorUpgradeNotAvailable)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeUnavailable, session.Translation.GetPokemonName(pokemon.PokemonId), pokemon.Cp, PokemonInfo.CalculateMaxCp(pokemon))
                });
            }
            else
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokemonUpgradeFailedError, session.Translation.GetPokemonName(pokemon.PokemonId))
                });
            }
        }
    }
}