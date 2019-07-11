#region using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Event.Egg;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Data;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class UseIncubatorsTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken, ObservableCollection<PokeEgg> eggCollection)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();

            await session.Inventory.RefreshPlayerStats();
            if (session.PlayerStats == null)
                return;

            var kmWalked = session.PlayerStats.KmWalked;

            var incubators = (await session.Inventory.GetEggIncubators())
                .Where(x => x.UsesRemaining > 0 || x.ItemId == ItemId.ItemIncubatorBasicUnlimited)
                .OrderByDescending(x => x.ItemId == ItemId.ItemIncubatorBasicUnlimited)
                .ToList();

            var allEggs = (await session.Inventory.GetEggs()).ToList();

            var unusedEggs = allEggs.Where(x => string.IsNullOrEmpty(x.EggIncubatorId))
                .OrderBy(x => x.EggKmWalkedTarget - x.EggKmWalkedStart)
                .ToList();

            if (eggCollection == null)
            {
                eggCollection = new ObservableCollection<PokeEgg>();
            }

            var rememberedIncubatorsFilePath = Path.Combine(session.LogicSettings.ProfilePath, "temp", "incubators.json");

            var pokemons = (await session.Inventory.GetPokemons()).ToList();

            // Check if eggs in remembered incubator usages have since hatched
            // (instead of calling session.Client.Inventory.GetHatchedEgg(), which doesn't seem to work properly)
            var rememberedIncubators = new List<IncubatorUsage>();

            if (eggCollection.Count == 0)
            {
                rememberedIncubators =
                    await CheckRememberedIncubators(session, cancellationToken, pokemons, rememberedIncubatorsFilePath);
            }
            else
            {
                await CheckHatchedFromEggList(session, cancellationToken, pokemons, eggCollection);
            }


            foreach (var e in allEggs)
            {
                if (eggCollection.All(x => x.EggId != e.Id))
                {
                    eggCollection.Add(new PokeEgg
                    {
                        Distance = e.EggKmWalkedTarget,
                        EggId = e.Id,
                        EggIncubatorId = e.EggIncubatorId,
                        WalkedDistance = 0,
                        TargetDistance = e.EggKmWalkedTarget
                    });
                }
            }

            var newRememberedIncubators = new List<IncubatorUsage>();
            foreach (var incubator in incubators.Where(x => x.ItemId == ItemId.ItemIncubatorBasicUnlimited ||
                                                            !session.LogicSettings.UseOnlyUnlimitedIncubator))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (incubator.PokemonId == 0)
                {
                    // Unlimited incubators prefer short eggs, limited incubators prefer long eggs
                    var egg = (incubator.ItemId == ItemId.ItemIncubatorBasicUnlimited &&
                               !session.LogicSettings.AlwaysPreferLongDistanceEgg)
                                ? unusedEggs.FirstOrDefault()
                                : unusedEggs.LastOrDefault();

                    if (egg == null)
                        continue;

                    var response = await session.Client.Inventory.UseItemEggIncubator(incubator.Id, egg.Id);
                    if (response?.EggIncubator == null) continue;
                    egg.EggIncubatorId = incubator.Id;
                    unusedEggs.Remove(egg);

                    newRememberedIncubators.Add(new IncubatorUsage {IncubatorId = incubator.Id, PokemonId = egg.Id});

                    session.EventDispatcher.Send(new EggIncubatorStatusEvent
                    {
                        IncubatorId = incubator.Id,
                        WasAddedNow = true,
                        PokemonId = egg.Id,
                        KmToWalk = egg.EggKmWalkedTarget,
                        KmRemaining = response.EggIncubator.TargetKmWalked - kmWalked
                    });

                    var targetEgg = eggCollection.FirstOrDefault(x => x.EggId == egg.Id);
                    if (targetEgg != null)
                    {
                        targetEgg.IncubatorType = incubator.ItemId;
                        targetEgg.EggIncubatorId = incubator.Id;
                        targetEgg.WalkedDistance = kmWalked;
                        targetEgg.TargetDistance = response.EggIncubator.TargetKmWalked;
                    }

                    await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
                }
                else
                {
                    newRememberedIncubators.Add(new IncubatorUsage
                    {
                        IncubatorId = incubator.Id,
                        PokemonId = incubator.PokemonId
                    });

                    session.EventDispatcher.Send(new EggIncubatorStatusEvent
                    {
                        IncubatorId = incubator.Id,
                        PokemonId = incubator.PokemonId,
                        KmToWalk = incubator.TargetKmWalked - incubator.StartKmWalked,
                        KmRemaining = incubator.TargetKmWalked - kmWalked
                    });

                    var targetEgg = eggCollection.FirstOrDefault(x => x.EggId == incubator.PokemonId);
                    if (targetEgg == null) continue;

                    targetEgg.IncubatorType = incubator.ItemId;
                    targetEgg.EggIncubatorId = incubator.Id;
                    targetEgg.WalkedDistance = kmWalked;
                    targetEgg.TargetDistance = incubator.TargetKmWalked;
                }
            }

            if (!newRememberedIncubators.SequenceEqual(rememberedIncubators))
                SaveRememberedIncubators(newRememberedIncubators, rememberedIncubatorsFilePath);
        }

        private static async Task CheckHatchedFromEggList(ISession session, CancellationToken cancellationToken, List<PokemonData> pokemons, ObservableCollection<PokeEgg> eggCollection)
        {
            var eggToRemove = new List<PokeEgg>();
            foreach (var egg in eggCollection)
            {
                var eggExists = pokemons.Any(x => x.Id == egg.EggId);
                if (!eggExists)
                {
                    eggToRemove.Add(egg);
                    continue;
                }

                var hatched = pokemons.FirstOrDefault(x => !x.IsEgg && x.Id == egg.EggId);
                if (hatched == null) continue;

                var pokemonSettings = await session.Inventory.GetPokemonSettings();
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

                var setting =
                    pokemonSettings.FirstOrDefault(q => q.PokemonId == hatched.PokemonId);
                var family = pokemonFamilies.FirstOrDefault(q => setting != null && q.FamilyId == setting.FamilyId);
                if (family == null || setting == null) continue;
                session.EventDispatcher.Send(new EggHatchedEvent
                {
                    Uid = hatched.Id,
                    Id = hatched.PokemonId,
                    Level = hatched.GetLevel(),
                    Cp = hatched.Cp,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(hatched, session.Runtime.CurrentLevel),
                    Perfection = Math.Round(hatched.CalculatePokemonPerfection(), 2),
                    Move1 = hatched.Move1,
                    Move2 = hatched.Move2,
                    Candy = family.Candy_,
                    Family = family.FamilyId,
                    Type1 = setting.Type,
                    Type2 = setting.Type2,
                    Stats = setting.Stats,
                    Stamina = hatched.Stamina,
                    IvSta = hatched.IndividualStamina,
                    PossibleCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(hatched, 40),
                    CandyToEvolve = setting.CandyToEvolve,
                    IvAtk = hatched.IndividualAttack,
                    IvDef = hatched.IndividualDefense,
                    Weight = hatched.WeightKg,
                    Cpm = hatched.CpMultiplier + hatched.AdditionalCpMultiplier,
                    StaminaMax = hatched.StaminaMax,
                    Evolutions = setting.EvolutionIds.ToArray()
                });
                eggToRemove.Add(egg);
                await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            }

            foreach (var egg in eggToRemove)
            {
                eggCollection.Remove(egg);
            }
        }

        private static async Task<List<IncubatorUsage>> CheckRememberedIncubators(ISession session, CancellationToken cancellationToken,
            List<PokemonData> pokemons, string rememberedIncubatorsFilePath)
        {
            var rememberedIncubators = GetRememberedIncubators(rememberedIncubatorsFilePath);
            if (rememberedIncubators == null) return new List<IncubatorUsage>();
            foreach (var incubator in rememberedIncubators)
            {
                var hatched = pokemons.FirstOrDefault(x => !x.IsEgg && x.Id == incubator.PokemonId);
                if (hatched == null) continue;

                var pokemonSettings = await session.Inventory.GetPokemonSettings();
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

                var setting =
                    pokemonSettings.FirstOrDefault(q => q.PokemonId == hatched.PokemonId);
                var family = pokemonFamilies.FirstOrDefault(q => setting != null && q.FamilyId == setting.FamilyId);
                if (family == null || setting == null) continue;
                session.EventDispatcher.Send(new EggHatchedEvent
                {
                    Uid = hatched.Id,
                    Id = hatched.PokemonId,
                    Level = hatched.GetLevel(),
                    Cp = hatched.Cp,
                    MaxCp = (int) PokemonInfo.GetMaxCpAtTrainerLevel(hatched, session.Runtime.CurrentLevel),
                    Perfection = Math.Round(hatched.CalculatePokemonPerfection(), 2),
                    Move1 = hatched.Move1,
                    Move2 = hatched.Move2,
                    Candy = family.Candy_,
                    Family = family.FamilyId,
                    Type1 = setting.Type,
                    Type2 = setting.Type2,
                    Stats = setting.Stats,
                    Stamina = hatched.Stamina,
                    IvSta = hatched.IndividualStamina,
                    PossibleCp = (int) PokemonInfo.GetMaxCpAtTrainerLevel(hatched, 40),
                    CandyToEvolve = setting.CandyToEvolve,
                    IvAtk = hatched.IndividualAttack,
                    IvDef = hatched.IndividualDefense,
                    Weight = hatched.WeightKg,
                    Cpm = hatched.CpMultiplier + hatched.AdditionalCpMultiplier,
                    StaminaMax = hatched.StaminaMax,
                    Evolutions = setting.EvolutionIds.ToArray()
                });
                await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            }

            return rememberedIncubators;
        }

        private static List<IncubatorUsage> GetRememberedIncubators(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
                return JsonConvert.DeserializeObject<List<IncubatorUsage>>(File.ReadAllText(filePath, Encoding.UTF8));

            return new List<IncubatorUsage>(0);
        }

        private static void SaveRememberedIncubators(List<IncubatorUsage> incubators, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            File.WriteAllText(filePath, JsonConvert.SerializeObject(incubators), Encoding.UTF8);
        }

        private class IncubatorUsage : IEquatable<IncubatorUsage>
        {
            public string IncubatorId;
            public ulong PokemonId;

            public bool Equals(IncubatorUsage other)
            {
                return other != null && other.IncubatorId == IncubatorId && other.PokemonId == PokemonId;
            }
        }
    }
}