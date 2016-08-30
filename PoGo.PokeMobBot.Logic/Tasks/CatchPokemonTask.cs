#region using directives

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Data;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CatchPokemonTask
    {
        private static readonly Random Rng = new Random();

        public static async Task<bool> Execute(ISession session, dynamic encounter, PokemonCacheItem pokemon, CancellationToken cancellationToken,
            FortData currentFortData = null, ulong encounterId = 0)
        {
            //if (!await CheckBotStateTask.Execute(session, cancellationToken)) return false;
            if (encounter is EncounterResponse && pokemon == null)
                throw new ArgumentException("Parameter pokemon must be set, if encounter is of type EncounterResponse",
                    nameof(pokemon));
            var prevState = session.State;
            session.State = BotState.Catch;
            var canUseBerry = true;
            CatchPokemonResponse caughtPokemonResponse;
            var attemptCounter = 1;
            do
            {
                if (session.LogicSettings.MaxPokeballsPerPokemon > 0 &&
                    attemptCounter > session.LogicSettings.MaxPokeballsPerPokemon)
                    break;

                float probability = encounter?.CaptureProbability?.CaptureProbability_[0];

                ItemId pokeball = await GetBestBall(session, encounter, probability);
                if (pokeball == ItemId.ItemUnknown)
                {
                    session.EventDispatcher.Send(new NoPokeballEvent
                    {
                        Id = encounter is EncounterResponse ? pokemon.PokemonId : encounter?.PokemonData.PokemonId,
                        Cp =
                            (encounter is EncounterResponse
                                ? encounter.WildPokemon?.PokemonData?.Cp
                                : encounter?.PokemonData?.Cp) ?? 0
                    });
                    session.State = prevState;
                    return false;
                }

                var useBerryBelowCatchProbability = session.LogicSettings.UseBerryBelowCatchProbability > 1
                    ? session.LogicSettings.UseBerryBelowCatchProbability / 100
                    : session.LogicSettings.UseBerryBelowCatchProbability;
                var isLowProbability = probability < useBerryBelowCatchProbability;
                var isHighCp = encounter != null &&
                               (encounter is EncounterResponse
                                   ? encounter.WildPokemon?.PokemonData?.Cp
                                   : encounter.PokemonData?.Cp) > session.LogicSettings.UseBerryMinCp;
                var isHighPerfection =
                    PokemonInfo.CalculatePokemonPerfection(encounter is EncounterResponse
                        ? encounter.WildPokemon?.PokemonData
                        : encounter?.PokemonData) >= session.LogicSettings.UseBerryMinIv;

                if (isLowProbability && ((session.LogicSettings.PrioritizeIvOverCp && isHighPerfection) || isHighCp) && canUseBerry)
                {
                    await
                        UseBerry(session,
                            encounter is EncounterResponse || encounter is IncenseEncounterResponse
                                ? pokemon.EncounterId
                                : encounterId,
                            encounter is EncounterResponse || encounter is IncenseEncounterResponse
                                ? pokemon.SpawnPointId
                                : currentFortData?.Id);
                    canUseBerry = false;
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 1000);
                }

                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude,
                    encounter is EncounterResponse || encounter is IncenseEncounterResponse
                        ? pokemon.Latitude
                        : currentFortData.Latitude,
                    encounter is EncounterResponse || encounter is IncenseEncounterResponse
                        ? pokemon.Longitude
                        : currentFortData.Longitude);

                double normalizedRecticleSize, spinModifier;
                if (session.LogicSettings.HumanizeThrows)
                {
                    normalizedRecticleSize =
                        Rng.NextInRange(session.LogicSettings.ThrowAccuracyMin, session.LogicSettings.ThrowAccuracyMax)*
                        1.85 + 0.1; // 0.1..1.95
                    if (normalizedRecticleSize > 1.95)
                        normalizedRecticleSize = 1.95;
                    else if (normalizedRecticleSize < 0.1)
                        normalizedRecticleSize = 0.1;
                    spinModifier = Rng.NextDouble() > session.LogicSettings.ThrowSpinFrequency ? 0.0 : 1.0;
                }
                else
                {
                    normalizedRecticleSize = 1.95;
                    spinModifier = 1.00;
                }
                Func<ItemId, string> returnRealBallName = a =>
                {
                    switch (a)
                    {
                        case ItemId.ItemPokeBall:
                            return session.Translation.GetTranslation(TranslationString.Pokeball);
                        case ItemId.ItemGreatBall:
                            return session.Translation.GetTranslation(TranslationString.GreatPokeball);
                        case ItemId.ItemUltraBall:
                            return session.Translation.GetTranslation(TranslationString.UltraPokeball);
                        case ItemId.ItemMasterBall:
                            return session.Translation.GetTranslation(TranslationString.MasterPokeball);
                        default:
                            return session.Translation.GetTranslation(TranslationString.CommonWordUnknown);
                    }
                };
                Func<double, string> getThrowType = a =>
                {
                    if (a < 1.0)
                         return "Normal ";
                    if (a < 1.3)
                        return "Nice! ";
                    if (a < 1.7)
                        return "Great! ";
                    return a > 1.6 ? "Excellent! " : "unknown ";
                };
                var hit = Rng.NextDouble() > session.LogicSettings.MissChance;
                Logger.Write($"Throwing {(Math.Abs(spinModifier - 1) < 0.00001 ?"Spinning " : "" )}{getThrowType(normalizedRecticleSize)}{returnRealBallName(pokeball)} - {(hit ? "WILL HIT" : "WILL MISS")}", Logging.LogLevel.Caught, session: session);
                caughtPokemonResponse =
                    await session.Client.Encounter.CatchPokemon(
                        encounter is EncounterResponse || encounter is IncenseEncounterResponse
                            ? pokemon.EncounterId
                            : encounterId,
                        encounter is EncounterResponse || encounter is IncenseEncounterResponse
                            ? pokemon.SpawnPointId
                            : currentFortData.Id, pokeball,
                        normalizedRecticleSize,
                        spinModifier, hitPokemon: hit);

                session.EventDispatcher.Send(new ItemLostEvent { Id = pokeball, Count = 1 });

                var lat = encounter is EncounterResponse || encounter is IncenseEncounterResponse
                    ? pokemon.Latitude
                    : currentFortData.Latitude;
                var lng = encounter is EncounterResponse || encounter is IncenseEncounterResponse
                    ? pokemon.Longitude
                    : currentFortData.Longitude;
                var evt = new PokemonCaptureEvent
                {
                    Status = caughtPokemonResponse.Status,
                    Latitude = lat,
                    Longitude = lng
                };

                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    if (pokemon != null)
					    pokemon.Caught = true;
                    evt.Uid = caughtPokemonResponse.CapturedPokemonId;

                    var totalExp = caughtPokemonResponse.CaptureAward.Xp.Sum();
                    var profile = await session.Client.Player.GetPlayer();

                    evt.Exp = totalExp;
                    evt.Stardust = profile.PlayerData.Currencies.ToArray()[1].Amount;

                    var pokemonSettings = await session.Inventory.GetPokemonSettings();
                    var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

                    var setting =
                        pokemonSettings.FirstOrDefault(q => q.PokemonId == pokemon?.PokemonId);
                    var family = pokemonFamilies.FirstOrDefault(q => setting != null && q.FamilyId == setting.FamilyId);

                    if (family != null)
                    {
                       

                        family.Candy_ += caughtPokemonResponse.CaptureAward.Candy.Sum();
                        evt.Family = family.FamilyId;
                        evt.FamilyCandies = family.Candy_;
                        evt.Type1 = setting.Type;
                        evt.Type2 = setting.Type2;
                        evt.Stats = setting.Stats;
                        evt.CandyToEvolve = setting.CandyToEvolve;
                        PokemonData poke = encounter is EncounterResponse ? encounter.WildPokemon?.PokemonData : encounter?.PokemonData;
                        if (poke != null)
                        {
                            evt.Stamina = poke.Stamina;
                            evt.MaxStamina = poke.StaminaMax;
                        }
                    }
                    else
                    {
                        evt.FamilyCandies = caughtPokemonResponse.CaptureAward.Candy.Sum();
                    }
                    session.MapCache.PokemonCaught(pokemon);

                    Logger.Write($"[Catch Dump] Caught {pokemon.PokemonId} - Coords[Lat: {lat} - Lng: {lng}]");
                }
                else if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchFlee)
                {
                    pokemon.Caught = true;
                }
                else if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape)
                {
                    canUseBerry = true;
                }

                evt.CatchType = encounter is EncounterResponse
                    ? session.Translation.GetTranslation(TranslationString.CatchTypeNormal)
                    : encounter is DiskEncounterResponse
                        ? session.Translation.GetTranslation(TranslationString.CatchTypeLure)
                        : session.Translation.GetTranslation(TranslationString.CatchTypeIncense);
                evt.Id = encounter is EncounterResponse ? pokemon.PokemonId : encounter?.PokemonData.PokemonId;

                var pokeData = (encounter is EncounterResponse
                    ? encounter.WildPokemon?.PokemonData
                    : encounter?.PokemonData) as PokemonData;

                if (pokeData != null)
                {
                    evt.Level = PokemonInfo.GetLevel(pokeData);
                    evt.Cp = pokeData.Cp;
                    evt.MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(pokeData, session.Runtime.CurrentLevel);
                    evt.Perfection = Math.Round(pokeData.CalculatePokemonPerfection(), 2);
                    evt.Probability =
                        Math.Round(probability*100, 2);

                    evt.Move1 = pokeData.Move1;
                    evt.Move2 = pokeData.Move2;

                    evt.PossibleCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(pokeData, 40);

                }
                evt.Distance = distance;
                evt.Pokeball = pokeball;
                evt.Attempt = attemptCounter;
                //await session.Inventory.RefreshCachedInventory();
                evt.BallAmount = await session.Inventory.GetItemAmountByType(pokeball);

                session.EventDispatcher.Send(evt);

                attemptCounter++;
                    await Task.Delay(session.LogicSettings.DelayCatchPokemon, cancellationToken);
            } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed ||
                     caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
            session.State = prevState;
            return true;
        }

        private static async Task<ItemId> GetBestBall(ISession session, dynamic encounter, float probability)
        {
            /*var pokemonCp = encounter is EncounterResponse //commented for possible future uses
                ? encounter.WildPokemon?.PokemonData?.Cp
                : encounter?.PokemonData?.Cp;*/
            var pokemonId = encounter is EncounterResponse
                ? encounter.WildPokemon?.PokemonData?.PokemonId
                : encounter?.PokemonData?.PokemonId;
            var iV =
                Math.Round(
                    PokemonInfo.CalculatePokemonPerfection(encounter is EncounterResponse
                        ? encounter.WildPokemon?.PokemonData
                        : encounter?.PokemonData));

            var useUltraBallBelowCatchProbability = session.LogicSettings.UseUltraBallBelowCatchProbability > 1
                ? session.LogicSettings.UseUltraBallBelowCatchProbability/100
                : session.LogicSettings.UseUltraBallBelowCatchProbability;
            var useGreatBallBelowCatchProbability = session.LogicSettings.UseGreatBallBelowCatchProbability > 1
                ? session.LogicSettings.UseGreatBallBelowCatchProbability/100
                : session.LogicSettings.UseGreatBallBelowCatchProbability;

            await session.Inventory.RefreshCachedInventory();
            var pokeBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            var greatBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            var ultraBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            var masterBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMasterBall);

            if (masterBallsCount > 0 && !session.LogicSettings.PokemonToUseMasterball.Any() ||
                session.LogicSettings.PokemonToUseMasterball.Contains(pokemonId))
                return ItemId.ItemMasterBall;
            if (ultraBallsCount > 0 && iV >= session.LogicSettings.UseUltraBallAboveIv ||
                ultraBallsCount > 0 && probability <= useUltraBallBelowCatchProbability)
                return ItemId.ItemUltraBall;
            if (greatBallsCount > 0 && iV >= session.LogicSettings.UseGreatBallAboveIv ||
                greatBallsCount > 0 && probability <= useGreatBallBelowCatchProbability)
                return ItemId.ItemGreatBall;
            //so we counted down, now if we don't have pokeballs we need to just use the best one available
            if (pokeBallsCount > 0)
                return ItemId.ItemPokeBall;
            else if (greatBallsCount > 0)
                return ItemId.ItemGreatBall;
            else if (ultraBallsCount > 0)
                return ItemId.ItemUltraBall;
            else
                return ItemId.ItemUnknown;
            //return pokeBallsCount > 0 ? ItemId.ItemPokeBall : ItemId.ItemUnknown;
        }

        private static async Task UseBerry(ISession session, ulong encounterId, string spawnPointId)
        {
            var inventoryBalls = await session.Inventory.GetItems();
            var berries = inventoryBalls.Where(p => p.ItemId == ItemId.ItemRazzBerry);
            var berry = berries.FirstOrDefault();

            if (berry == null || berry.Count <= 0)
                return;

            session.EventDispatcher.Send(new ItemLostEvent { Id = ItemId.ItemRazzBerry, Count = 1 });
            await session.Client.Encounter.UseCaptureItem(encounterId, ItemId.ItemRazzBerry, spawnPointId);
            berry.Count -= 1;
            session.EventDispatcher.Send(new UseBerryEvent {Count = berry.Count});
        }
    }
}
