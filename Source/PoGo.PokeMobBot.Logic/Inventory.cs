#region using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public class Inventory
    {
        private readonly Client _client;
        private readonly ILogicSettings _logicSettings;
        private readonly ITranslation _translation;

        private GetInventoryResponse _cachedInventory;
        private long _lastRefresh;

        public PlayerStats PlayerStats;
        public ObservableCollection<PokeDexRecord> PokeDex;

        public Inventory(Client client, ILogicSettings logicSettings, ITranslation translation)
        {
            _client = client;
            _logicSettings = logicSettings;
            _translation = translation;
        }

        public void RemoveItemFromCacheByType(ItemId type, int amount)
        {
            var item =
                _cachedInventory.InventoryDelta?.InventoryItems?.FirstOrDefault(
                    x => x?.InventoryItemData?.Item?.ItemId == type);
            if (item?.InventoryItemData?.Item != null)
            {
                item.InventoryItemData.Item.Count -= amount;
            }
        }

        public async Task DeletePokemonFromInvById(ulong id)
        {
            var inventory = await GetCachedInventory();
            var pokemon =
                inventory.InventoryDelta.InventoryItems.FirstOrDefault(
                    i => i.InventoryItemData.PokemonData != null && i.InventoryItemData.PokemonData.Id == id);
            if (pokemon != null)
                inventory.InventoryDelta.InventoryItems.Remove(pokemon);
        }

        public async Task<List<AppliedItem>> GetUsedItems()
        {
            var inventory = await GetCachedInventory();
            var appliedItems =
                inventory?.InventoryDelta?.InventoryItems?.Where(x=> x.InventoryItemData?.AppliedItems?.Item?.Count > 0).Select(x=>x.InventoryItemData?.AppliedItems?.Item?.ToList());
            if (appliedItems == null) return new List<AppliedItem>();
            List<AppliedItem> appliedItemses = new List<AppliedItem>();
            foreach (var applied in appliedItems)
            {
                appliedItemses.AddRange(applied);
            }
            appliedItemses = appliedItemses.Where(x => x.ExpireMs > DateTime.UtcNow.ToUnixTime()).ToList();
            return appliedItemses;
        }

        private async Task<GetInventoryResponse> GetCachedInventory()
        {
            if (_lastRefresh + 30000 > DateTime.UtcNow.ToUnixTime() && _cachedInventory != null && _cachedInventory.Success)
            {
                return _cachedInventory;
            }
            return await RefreshCachedInventory();
        }

        public async Task<LevelUpRewardsResponse> GetLevelUpRewards(int level)
        {
            var rewards = await _client.Player.GetLevelUpRewards(level);

            return rewards;
        }
        public List<PokemonData> GetDuplicatePokemonToTransferList(IEnumerable<PokemonData> myPokemon)
        {
            var pokemonList = new List<PokemonData>();

            if (myPokemon == null) return pokemonList;

            var pokemonDatas = myPokemon as IList<PokemonData> ?? myPokemon.ToList();
            var pokeByType = pokemonDatas.GroupBy(x => x.PokemonId);
            var ivOverCp = _logicSettings.PrioritizeIvOverCp || _logicSettings.PrioritizeBothIvAndCpForTransfer;
            foreach (var pokeGroup in pokeByType)
            {
                var typeFilter = GetPokemonTransferFilter(pokeGroup.Key);
                var duplicatePoke =
                    pokeGroup.OrderByDescending(x => ivOverCp ? x.CalculatePokemonPerfection() : x.Cp)
                        .ThenByDescending(x => ivOverCp ? x.Cp : x.CalculatePokemonPerfection())
                        .Skip(typeFilter.KeepMinDuplicatePokemon);
                if (duplicatePoke.Any())
                {
                    duplicatePoke = duplicatePoke.Where(x => string.IsNullOrEmpty(x.DeployedFortId) && x.Favorite == 0);
                    if (!duplicatePoke.Any()) continue;
                    if (_logicSettings.PrioritizeBothIvAndCpForTransfer)
                    {
                        duplicatePoke =
                            duplicatePoke.Where(x => x.Cp < typeFilter.KeepMinCp &&
                                                     x.CalculatePokemonPerfection() < typeFilter.KeepMinIvPercentage);
                    }
                    else if (_logicSettings.PrioritizeIvOverCp)
                    {
                        duplicatePoke =
                            duplicatePoke.Where(
                                x => x.CalculatePokemonPerfection() < typeFilter.KeepMinIvPercentage);
                    }
                    else
                    {
                        duplicatePoke =
                            duplicatePoke.Where(x => x.Cp < typeFilter.KeepMinCp);
                    }
                }
                pokemonList.AddRange(duplicatePoke);
            }
            return pokemonList;
        }

        public async Task<IEnumerable<PokemonData>> GetDuplicatePokemonToTransfer(
            bool keepPokemonsThatCanEvolve = false, bool prioritizeIVoverCp = false,
            IEnumerable<PokemonId> filter = null)
        {

            var myPokemon = await GetPokemons();

            var pokemonList = GetDuplicatePokemonToTransferList(myPokemon);

            if (filter != null)
            {
                pokemonList = pokemonList?.Where(p => !filter.Contains(p.PokemonId)).ToList();
            }
            if (!keepPokemonsThatCanEvolve) return pokemonList;

            var results = new List<PokemonData>();
            var pokemonsThatCanBeTransfered = pokemonList?.GroupBy(p => p.PokemonId).ToList();

            var myPokemonSettings = await GetPokemonSettings();
            var pokemonSettings = myPokemonSettings.ToList();

            var myPokemonFamilies = await GetPokemonFamilies();
            var pokemonFamilies = myPokemonFamilies.ToArray();

            if (pokemonsThatCanBeTransfered == null) return results;

            foreach (var pokemon in pokemonsThatCanBeTransfered)
            {
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.Key);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);
                var amountToSkip = GetPokemonTransferFilter(pokemon.Key).KeepMinDuplicatePokemon;

                if (settings.CandyToEvolve > 0 && _logicSettings.PokemonsToEvolve.Contains(pokemon.Key))
                {
                    var amountPossible = (familyCandy.Candy_ - 1)/(settings.CandyToEvolve - 1);

                    if (amountPossible > amountToSkip)
                        amountToSkip = amountPossible;
                }

                if (prioritizeIVoverCp)
                {
                    results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                        .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                        .ThenByDescending(n => n.Cp)
                        .Skip(amountToSkip)
                        .ToList());
                }
                else
                {
                    results.AddRange(pokemonList.Where(x => x.PokemonId == pokemon.Key)
                        .OrderByDescending(x => x.Cp)
                        .ThenByDescending(n => n.CalculatePokemonPerfection())
                        .Skip(amountToSkip)
                        .ToList());
                }
            }


            return results;

        }

        public async Task<IEnumerable<EggIncubator>> GetEggIncubators()
        {
            var inventory = await GetCachedInventory();
            return
                inventory.InventoryDelta.InventoryItems
                    .Where(x => x.InventoryItemData.EggIncubators != null)
                    .SelectMany(i => i.InventoryItemData.EggIncubators.EggIncubator)
                    .Where(i => i != null);
        }

        public async Task<IEnumerable<PokemonData>> GetEggs()
        {
            var inventory = await GetCachedInventory();
            return
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonData)
                    .Where(p => p != null && p.IsEgg);
        }

        public async Task<PokemonData> GetHighestPokemonOfTypeByCp(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(x => x.Cp)
                .ThenByDescending(PokemonInfo.CalculatePokemonPerfection)
                .FirstOrDefault();
        }
        public async Task<int> GetStarDust()
        {
            var starDust =await  _client.Player.GetPlayer();
            var gdrfds = starDust.PlayerData.Currencies;
            var splitStar = gdrfds[1].Amount;
            return splitStar;

        }

        public async Task<PokemonData> GetHighestPokemonOfTypeByIv(PokemonData pokemon)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon.ToList();
            return pokemons.Where(x => x.PokemonId == pokemon.PokemonId)
                .OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                .ThenByDescending(x => x.Cp)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<PokemonData>> GetHighestsCp(int limit)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon?.ToList();
            return pokemons?.OrderByDescending(x => x?.Cp)
                .ThenByDescending(PokemonInfo.CalculatePokemonPerfection)
                .Take(limit);
        }

        public async Task<IEnumerable<PokemonData>> GetHighestsPerfect(int limit)
        {
            var myPokemon = await GetPokemons();
            var pokemons = myPokemon?.ToList();
            return pokemons?.OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                .ThenByDescending(x => x.Cp)
                .Take(limit);
        }


        public async Task<int> GetItemAmountByType(ItemId type)
        {
            var pokeballs = await GetItems();
            return pokeballs.FirstOrDefault(i => i.ItemId == type)?.Count ?? 0;
        }

        public async Task<IEnumerable<ItemData>> GetItems()
        {
            var inventory = await GetCachedInventory();
            return inventory?.InventoryDelta?.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .Where(p => p != null);
        }

        public async Task<int> GetTotalItemCount()
        {
            var myItems = (await GetItems())?.ToList();
            return myItems?.Sum(myItem => myItem.Count) ?? 0;
        }

        public async Task<IEnumerable<ItemData>> GetItemsToRecycle(ISession session)
        {
            await session.Inventory.RefreshCachedInventory();
            var itemsToRecycle = new List<ItemData>();
            var myItems = (await GetItems()).ToList();

            var currentAmountOfPokeballs = await GetItemAmountByType(ItemId.ItemPokeBall);
            var currentAmountOfGreatballs = await GetItemAmountByType(ItemId.ItemGreatBall);
            var currentAmountOfUltraballs = await GetItemAmountByType(ItemId.ItemUltraBall);
            var currentAmountOfMasterballs = await GetItemAmountByType(ItemId.ItemMasterBall);
            var totalBalls = currentAmountOfPokeballs + currentAmountOfGreatballs
                + currentAmountOfUltraballs + currentAmountOfMasterballs;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentPokeballInv,
                    currentAmountOfPokeballs, currentAmountOfGreatballs, currentAmountOfUltraballs,
                    currentAmountOfMasterballs, totalBalls)
            });

            var currentAmountOfPotions = await GetItemAmountByType(ItemId.ItemPotion);
            var currentAmountOfSuperPotions = await GetItemAmountByType(ItemId.ItemSuperPotion);
            var currentAmountOfHyperPotions = await GetItemAmountByType(ItemId.ItemHyperPotion);
            var currentAmountOfMaxPotions= await GetItemAmountByType(ItemId.ItemMaxPotion);
            var totalPotions = currentAmountOfPotions + currentAmountOfSuperPotions
                + currentAmountOfHyperPotions + currentAmountOfMaxPotions;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentPotionInv,
                    currentAmountOfPotions, currentAmountOfSuperPotions, currentAmountOfHyperPotions,
                    currentAmountOfMaxPotions, totalPotions)
            });

            var currentAmountofRazz = await GetItemAmountByType(ItemId.ItemRazzBerry);
            var currentAmountofBluk = await GetItemAmountByType(ItemId.ItemBlukBerry);
            var currentAmountofNanab = await GetItemAmountByType(ItemId.ItemNanabBerry);
            var currentAmountofPinap = await GetItemAmountByType(ItemId.ItemPinapBerry);
            var currentAmountofWepar = await GetItemAmountByType(ItemId.ItemWeparBerry);
            var totalBerries = currentAmountofRazz + currentAmountofBluk
                + currentAmountofNanab + currentAmountofPinap + currentAmountofWepar;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentBerryInv,
                    currentAmountofRazz, currentAmountofBluk, currentAmountofNanab,
                    currentAmountofPinap, currentAmountofWepar, totalBerries)
            });

            var currentAmountofRevive = await GetItemAmountByType(ItemId.ItemRevive);
            var currentAmountofMaxRevive = await GetItemAmountByType(ItemId.ItemMaxRevive);
            var totalRevives = currentAmountofRevive + currentAmountofMaxRevive;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentReviveInv,
                    currentAmountofRevive, currentAmountofMaxRevive, totalRevives)
            });

            var currentAmountofIncense = await GetItemAmountByType(ItemId.ItemIncenseOrdinary);
            var currentAmountofIncenseCool = await GetItemAmountByType(ItemId.ItemIncenseCool);
            var currentAmountofIncenseFloral = await GetItemAmountByType(ItemId.ItemIncenseFloral);
            var currentAmountofIncenseSpicy = await GetItemAmountByType(ItemId.ItemIncenseSpicy);
            var totalIncense = currentAmountofIncense + currentAmountofIncenseCool
                + currentAmountofIncenseFloral + currentAmountofIncenseSpicy;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentIncenseInv,
                    currentAmountofIncense, currentAmountofIncenseCool, currentAmountofIncenseFloral, 
                    currentAmountofIncenseSpicy, totalIncense)
            });

            var currentAmountofLures = await GetItemAmountByType(ItemId.ItemTroyDisk);
            var currentAmountofLuckyEggs = await GetItemAmountByType(ItemId.ItemLuckyEgg);
            var currentAmountofIncubators = await GetItemAmountByType(ItemId.ItemIncubatorBasic);
            var currentMisc = currentAmountofLures + currentAmountofLuckyEggs + currentAmountofIncubators;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentMiscInv,
                    currentAmountofLures, currentAmountofLuckyEggs, currentAmountofIncubators, currentMisc)
            });

            var currentInvUsage = await session.Inventory.GetTotalItemCount();
            var maxInvUsage = session.Profile.PlayerData.MaxItemStorage;

            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentInvUsage, currentInvUsage, maxInvUsage)
            });

            var otherItemsToRecycle = myItems
                .Where(x => _logicSettings.ItemRecycleFilter.Any(f => f.Key == x.ItemId && x.Count > f.Value))
                .Select(
                    x =>
                        new ItemData
                        {
                            ItemId = x.ItemId,
                            Count = x.Count - _logicSettings.ItemRecycleFilter.Single(f => f.Key == x.ItemId).Value,
                            Unseen = x.Unseen
                        });
            itemsToRecycle.AddRange(otherItemsToRecycle);
            return itemsToRecycle;
        }

        public double GetPerfect(PokemonData poke)
        {
            var result = poke.CalculatePokemonPerfection();
            return result;
        }

        public async Task<IEnumerable<PlayerStats>> GetPlayerStats()
        {
            var inventory = await GetCachedInventory();
            return inventory?.InventoryDelta?.InventoryItems
                .Select(i => i.InventoryItemData?.PlayerStats)
                .Where(p => p != null);
        }

        public async Task<PlayerStats> RefreshPlayerStats()
        {
            var inventory = await GetCachedInventory();
            if (inventory?.InventoryDelta == null) return null;

            PlayerStats = (inventory?.InventoryDelta?.InventoryItems
                .Select(i => i.InventoryItemData?.PlayerStats)).FirstOrDefault(p => p != null);
            return PlayerStats;
        }

        public async Task<int> GetPokedexCount()
        {
            var hfgds = await GetCachedInventory();

            return hfgds.InventoryDelta.InventoryItems.Count(t => t.ToString().ToLower().Contains("pokedex"));
        }

        public async Task UpdatePokeDex()
        {
            var inventory = await GetCachedInventory();

            var newPokeDex = (from items in inventory?.InventoryDelta.InventoryItems
                   where items.InventoryItemData?.PokedexEntry != null
                   select items).Select(x=>x.InventoryItemData.PokedexEntry).ToList();

            foreach (var entry in newPokeDex)
            {
                var dexEntry = PokeDex.FirstOrDefault(x => x.Id == entry.PokemonId);
                if (dexEntry == null)
                {
                    PokeDex.Add(new PokeDexRecord
                    {
                        Id = entry.PokemonId,
                        SeenTimes = entry.TimesEncountered,
                        CapturedTimes = entry.TimesCaptured,
                        PokemonName = _translation.GetPokemonName(entry.PokemonId)
                    });
                }
                else
                {
                    dexEntry.CapturedTimes = entry.TimesCaptured;
                    dexEntry.SeenTimes = entry.TimesEncountered;
                }
            }

        }

        public async Task<List<Candy>> GetPokemonFamilies()
        {
            var inventory = await GetCachedInventory();

            if (inventory?.InventoryDelta == null) return new List<Candy>();

            var families = from item in inventory.InventoryDelta?.InventoryItems
                where item?.InventoryItemData?.Candy != null
                where item.InventoryItemData?.Candy.FamilyId != PokemonFamilyId.FamilyUnset
                group item by item?.InventoryItemData?.Candy.FamilyId
                into family
                select new Candy
                {
                    FamilyId = family.First().InventoryItemData.Candy.FamilyId,
                    Candy_ = family.First().InventoryItemData.Candy.Candy_
                };


            return families.ToList();
        }

        public async Task<IEnumerable<PokemonData>> GetPokemons()
        {
            var inventory = await GetCachedInventory();
            return
                inventory?.InventoryDelta?.InventoryItems?.Select(i => i.InventoryItemData?.PokemonData)
                    .Where(p => p != null && p.PokemonId > 0);
        }

        public async Task<int> GetPokemonsCount()
        {
            var inventory = await GetCachedInventory();
            if (inventory?.InventoryDelta != null)
                return
                    inventory.InventoryDelta.InventoryItems
                        .Select(i => i.InventoryItemData?.PokemonData).Count(p => p != null && p.PokemonId > 0);
            return 0;
        }

        public async Task<List<PokemonSettings>> GetPokemonSettings()
        {
            var templates = await _client.Download.GetItemTemplates();
            return
                templates.ItemTemplates.Select(i => i.PokemonSettings)
                    .Where(p => p != null && p.FamilyId != PokemonFamilyId.FamilyUnset).ToList();
        }

        public async Task<IEnumerable<PokemonUpgradeSettings>> GetPokemonUpgradeSettings()
        {
            var templates = await _client.Download.GetItemTemplates();
            return templates.ItemTemplates.Select(i => i.PokemonUpgrades).Where(p => p != null);
        }

        public async Task<IEnumerable<PokemonData>> GetPokemonToEvolve(ILogicSettings logic, IEnumerable<PokemonId> filter = null)
        {
            var myPokemons = await GetPokemons();
            myPokemons = myPokemons.Where(p => string.IsNullOrEmpty(p.DeployedFortId));
            if (logic.PrioritizeIvOverCp || logic.PrioritizeBothIvAndCpForTransfer)
            {
                myPokemons = myPokemons.OrderByDescending(p => p.CalculatePokemonPerfection());
            }
            else
            {
                myPokemons = myPokemons.OrderByDescending(x => x.Cp);
            }
            //Don't evolve pokemon in gyms
            if (filter != null)
            {
                IEnumerable<PokemonId> pokemonIds = filter as PokemonId[] ?? filter.ToArray();
                if (pokemonIds.Any())
                {
                    myPokemons =
                        myPokemons.Where(
                            p => (_logicSettings.EvolveAllPokemonWithEnoughCandy && pokemonIds.Contains(p.PokemonId)) ||
                                 (_logicSettings.EvolveAllPokemonAboveIv &&
                                  (p.CalculatePokemonPerfection() >= _logicSettings.EvolveAboveIvValue)));
                }
                else if (_logicSettings.EvolveAllPokemonAboveIv)
                {
                    myPokemons =
                        myPokemons.Where(
                            p => p.CalculatePokemonPerfection() >= _logicSettings.EvolveAboveIvValue);
                }
            }
            var pokemons = myPokemons.ToList();

            var myPokemonSettings = await GetPokemonSettings();
            var pokemonSettings = myPokemonSettings.ToList();

            var myPokemonFamilies = await GetPokemonFamilies();
            var pokemonFamilies = myPokemonFamilies.ToArray();

            var pokemonToEvolve = new List<PokemonData>();
            foreach (var pokemon in pokemons)
            {
                var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);

                //Don't evolve if we can't evolve it
                if (settings.EvolutionIds.Count == 0)
                    continue;

                var pokemonCandyNeededAlready =
                    pokemonToEvolve.Count(
                        p => pokemonSettings.Single(x => x.PokemonId == p.PokemonId).FamilyId == settings.FamilyId) *
                    settings.CandyToEvolve;

                if (familyCandy.Candy_ - pokemonCandyNeededAlready > settings.CandyToEvolve)
                {
                    pokemonToEvolve.Add(pokemon);
                }
            }

            return pokemonToEvolve;
        }

        public TransferFilter GetPokemonTransferFilter(PokemonId pokemon)
        {
            if (_logicSettings.PokemonsTransferFilter != null &&
                _logicSettings.PokemonsTransferFilter.Any(x=>x.Id == pokemon))
            {
                return _logicSettings.PokemonsTransferFilter.FirstOrDefault(x=>x.Id == pokemon);
            }
            return new TransferFilter(pokemon, _logicSettings.KeepMinCp, _logicSettings.KeepMinIvPercentage,
                _logicSettings.KeepMinDuplicatePokemon);
        }

        public async Task<PokemonData> GetBuddyPokemon(ulong id)
        {
            var allPoke = await GetPokemons();
            var targetPoke = allPoke?.FirstOrDefault(x => x.Id == id);
            return targetPoke;
        }

        public async Task<GetInventoryResponse> RefreshCachedInventory(bool force = false)
        {
            if (force) _lastRefresh = 0;
            var ss = new SemaphoreSlim(10);
            await ss.WaitAsync();
            try
            {
                var invDiffs =
                    await
                        _client.Inventory.GetInventory(_lastRefresh);
                _lastRefresh = DateTime.UtcNow.ToUnixTime();

                if (invDiffs?.InventoryDelta?.InventoryItems == null || !invDiffs.Success ||
                    invDiffs.InventoryDelta.InventoryItems.Count <= 0) return _cachedInventory;

                _lastRefresh = invDiffs.InventoryDelta.NewTimestampMs;

                if (_cachedInventory?.InventoryDelta?.InventoryItems == null)
                {
                    _cachedInventory = invDiffs;
                }
                else
                {
                    foreach (var dif in invDiffs.InventoryDelta.InventoryItems)
                    {
                        if (dif?.DeletedItem != null)
                        {
                            var pokeToRemove =
                                _cachedInventory.InventoryDelta.InventoryItems.FirstOrDefault(
                                    x => x.InventoryItemData?.PokemonData?.Id ==
                                         dif.DeletedItem.PokemonId);
                            if (pokeToRemove != null)
                                _cachedInventory.InventoryDelta.InventoryItems.Remove(pokeToRemove);
                            continue;
                        }

                        List<InventoryItem> cachedDataToRemove = new List<InventoryItem>();
                        // if (dif.InventoryItemData.AppliedItems != null) //All new so dont need to remove old

                        if (dif?.InventoryItemData == null) continue;
                        if (dif.InventoryItemData.Candy != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x =>
                                        x.InventoryItemData.Candy?.FamilyId ==
                                        dif.InventoryItemData.Candy.FamilyId));

                        if (dif.InventoryItemData.EggIncubators != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.EggIncubators != null));

                        if (dif.InventoryItemData.InventoryUpgrades != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.InventoryUpgrades != null));

                        if (dif.InventoryItemData.Item != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.Item?.ItemId == dif.InventoryItemData.Item.ItemId));

                        if (dif.InventoryItemData.PlayerCamera != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.PlayerCamera != null));

                        if (dif.InventoryItemData.PlayerCurrency != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.PlayerCurrency != null));

                        if (dif.InventoryItemData.PlayerStats != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x => x.InventoryItemData.PlayerStats != null));

                        if (dif.InventoryItemData.PokedexEntry != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x =>
                                        x.InventoryItemData.PokedexEntry?.PokemonId ==
                                        dif.InventoryItemData.PokedexEntry.PokemonId));

                        if (dif.InventoryItemData.PokemonData != null)
                            cachedDataToRemove.AddRange(
                                _cachedInventory.InventoryDelta.InventoryItems.Where(
                                    x =>
                                        x.InventoryItemData.PokemonData?.Id ==
                                        dif.InventoryItemData.PokemonData.Id));

                        foreach (var itm in cachedDataToRemove)
                        {
                            _cachedInventory.InventoryDelta.InventoryItems.Remove(itm);
                        }

                        _cachedInventory.InventoryDelta.InventoryItems.Add(dif);
                    }
                }
                return _cachedInventory;
            }
            catch
            {
                return _cachedInventory;
            }
            finally
            {
                ss.Release();
            }
        }

        public async Task<UpgradePokemonResponse> UpgradePokemon(ulong pokemonid)
        {
            var upgradeResult = await _client.Inventory.UpgradePokemon(pokemonid);
            return upgradeResult;
        }
        public async Task<SetFavoritePokemonResponse> SetFavoritePokemon(ulong pokemonid, bool favorite)
        {
            var favoriteResult = await _client.Inventory.SetFavoritePokemon(pokemonid, favorite);
            return favoriteResult;
        }

        public async Task<Tuple<bool,long>> UseItem(ItemId item, CancellationToken token)
        {
            var usedItems = await GetUsedItems();
            if (usedItems.Any(x => x.ItemId == item && x.ExpireMs > DateTime.UtcNow.ToUnixTime()))
            {
                return Tuple.Create(false, (long)0);
            }

            var inventory = (await GetCachedInventory()).InventoryDelta;
            if (!inventory.InventoryItems.Any(
                    x => x?.InventoryItemData?.Item?.ItemId == item && x?.InventoryItemData?.Item?.Count > 0))
            {
                return Tuple.Create(false, (long)0);
            }

            switch (item)
            {
                case ItemId.ItemLuckyEgg:
                    var respLucky = await _client.Inventory.UseItemXpBoost();
                    var appliedLucky = respLucky.AppliedItems.Item.Where(x => x.ItemId == ItemId.ItemLuckyEgg).ToList().FirstOrDefault();
                    if (appliedLucky == null) return Tuple.Create(false, (long)0);
                    return Tuple.Create(respLucky.Result == UseItemXpBoostResponse.Types.Result.Success,appliedLucky.ExpireMs);
                case ItemId.ItemIncenseOrdinary:
                    var respInc1 = await _client.Inventory.UseIncense(item);
                    return Tuple.Create(respInc1.Result == UseIncenseResponse.Types.Result.Success, respInc1.AppliedIncense.ExpireMs);
                case ItemId.ItemIncenseSpicy:
                    var respInc2 = await _client.Inventory.UseIncense(item);
                    return Tuple.Create(respInc2.Result == UseIncenseResponse.Types.Result.Success, respInc2.AppliedIncense.ExpireMs);
                case ItemId.ItemIncenseCool:
                    var respInc3 = await _client.Inventory.UseIncense(item);
                    return Tuple.Create(respInc3.Result == UseIncenseResponse.Types.Result.Success, respInc3.AppliedIncense.ExpireMs);
                case ItemId.ItemPokemonStorageUpgrade:
                    break;
                case ItemId.ItemItemStorageUpgrade:
                    break;
                default:
                    return Tuple.Create(false, (long)0); 
            }
            return Tuple.Create(false, (long)0);
        }
    }
}