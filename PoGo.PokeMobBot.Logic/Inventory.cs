#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PokemonGo.RocketAPI;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;
using PoGo.PokeMobBot.Logic.Utils;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public class Inventory
    {
        private readonly Client _client;
        private readonly ILogicSettings _logicSettings;

        private GetInventoryResponse _cachedInventory;
        private DateTime _lastRefresh;

        public Inventory(Client client, ILogicSettings logicSettings)
        {
            _client = client;
            _logicSettings = logicSettings;
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

        public async Task<List<AppliedItem>>  GetUsedItems()
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
            return appliedItemses;
        }

        private async Task<GetInventoryResponse> GetCachedInventory()
        {
            var now = DateTime.UtcNow;

            if (_lastRefresh.AddSeconds(30).Ticks > now.Ticks && _cachedInventory != null && _cachedInventory.Success)
            {
                return _cachedInventory;
            }
            return await RefreshCachedInventory();
        }

        public async Task<LevelUpRewardsResponse> GetLevelUpRewards(StatsExport playerStats)
        {
            var rewards = await _client.Player.GetLevelUpRewards(playerStats.Level);

            return rewards;
        }
        public List<PokemonData> GetDuplicatePokemonToTransferList(IEnumerable<PokemonData> myPokemon)
        {
            List<PokemonData> pokemonList;
            if (_logicSettings.PrioritizeBothIvAndCpForTransfer)
            {
                pokemonList =
                myPokemon?.Where(
                    p => p.DeployedFortId == string.Empty &&
                         p.Favorite == 0 && (p.Cp < GetPokemonTransferFilter(p.PokemonId).KeepMinCp &&
                                             p.CalculatePokemonPerfection() <
                                             GetPokemonTransferFilter(p.PokemonId).KeepMinIvPercentage))
                    .ToList();
            }
            else if (!_logicSettings.PrioritizeIvOverCp)
            {
                pokemonList =
                myPokemon?.Where(
                    p => p.DeployedFortId == string.Empty &&
                         p.Favorite == 0 && (p.Cp < GetPokemonTransferFilter(p.PokemonId).KeepMinCp ||
                                             p.CalculatePokemonPerfection() <
                                             GetPokemonTransferFilter(p.PokemonId).KeepMinIvPercentage))
                    .ToList();
            }
            else
            {
                pokemonList =
                myPokemon?.Where(
                    p => p.DeployedFortId == string.Empty &&
                         p.Favorite == 0 && (p.CalculatePokemonPerfection() <
                                             GetPokemonTransferFilter(p.PokemonId).KeepMinIvPercentage) ||
                                             p.Cp < GetPokemonTransferFilter(p.PokemonId).KeepMinCp)
                    .ToList();
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
            if (keepPokemonsThatCanEvolve)
            {
                var results = new List<PokemonData>();
                var pokemonsThatCanBeTransfered = pokemonList?.GroupBy(p => p.PokemonId)
                    .Where(x => x.Count() > GetPokemonTransferFilter(x.Key).KeepMinDuplicatePokemon).ToList();

                var myPokemonSettings = await GetPokemonSettings();
                var pokemonSettings = myPokemonSettings.ToList();

                var myPokemonFamilies = await GetPokemonFamilies();
                var pokemonFamilies = myPokemonFamilies.ToArray();

                if (pokemonsThatCanBeTransfered == null) return results;
                {
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
                }

                return results;
            }
            if (prioritizeIVoverCp)
            {
                return pokemonList?
                    .GroupBy(p => p.PokemonId)
                    .Where(x => x.Any())
                    .SelectMany(
                        p =>
                            p.OrderByDescending(PokemonInfo.CalculatePokemonPerfection)
                                .ThenByDescending(n => n.Cp)
                                .Skip(GetPokemonTransferFilter(p.Key).KeepMinDuplicatePokemon)
                                .ToList());
            }
            return pokemonList?
                .GroupBy(p => p.PokemonId)
                .Where(x => x.Any())
                .SelectMany(
                    p =>
                        p.OrderByDescending(x => x.Cp)
                            .ThenByDescending(n => n.CalculatePokemonPerfection())
                            .Skip(GetPokemonTransferFilter(p.Key).KeepMinDuplicatePokemon)
                            .ToList());
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
            return pokemons?.OrderByDescending(x => x.Cp)
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

        public async Task<int> GetPokedexCount()
        {
            var hfgds = await _client.Inventory.GetInventory();

            return hfgds.InventoryDelta.InventoryItems.Count(t => t.ToString().ToLower().Contains("pokedex"));
        }

        public async Task<List<InventoryItem>> GetPokeDexItems()
        {
            var inventory = await _client.Inventory.GetInventory();

            return (from items in inventory?.InventoryDelta.InventoryItems
                   where items.InventoryItemData?.PokedexEntry != null
                   select items).ToList();
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

        public async Task<IEnumerable<PokemonData>> GetPokemonToEvolve(IEnumerable<PokemonId> filter = null)
        {
            var myPokemons = await GetPokemons();
            myPokemons = myPokemons.Where(p => p.DeployedFortId == string.Empty).OrderByDescending(p => p.Cp);
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
                _logicSettings.PokemonsTransferFilter.ContainsKey(pokemon))
            {
                return _logicSettings.PokemonsTransferFilter[pokemon];
            }
            return new TransferFilter(_logicSettings.KeepMinCp, _logicSettings.KeepMinIvPercentage,
                _logicSettings.KeepMinDuplicatePokemon);
        }

        public async Task<GetInventoryResponse> RefreshCachedInventory()
        {
            var now = DateTime.UtcNow;
            var ss = new SemaphoreSlim(10);

            await ss.WaitAsync();
            try
            {
                _lastRefresh = now;
                _cachedInventory = await _client.Inventory.GetInventory();
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
    }
}