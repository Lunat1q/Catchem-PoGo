#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class FarmPokestopsTask
    {
        public static int TimesZeroXPawarded;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            if (session.LogicSettings.Teleport)
                await Teleport(session, cancellationToken);
            else
                await NoTeleport(session, cancellationToken);

        }
        public static async Task Teleport(ISession session, CancellationToken cancellationToken)
        {
            int stopsToHit = 20; //We should return to the main loop after some point, might as well limit this.
            //Not sure where else we could put this? Configs maybe if we incorporate
            //deciding how many pokestops in a row we want to hit before doing things like recycling?
            //might increase xp/hr not stopping every 5 stops. - Pocket


            //TODO: run through this with a fine-tooth comb and optimize it.
            var pokestopList = await GetPokeStops(session);
            for (int stopsHit = 0; stopsHit < stopsToHit; stopsHit++) {
                if (pokestopList.Count > 0)
                {
                    //start at 0 ends with 19 = 20 for the leechers{
                    cancellationToken.ThrowIfCancellationRequested();

                    var distanceFromStart = LocationUtils.CalculateDistanceInMeters(
                        session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                        session.Client.CurrentLatitude, session.Client.CurrentLongitude);

                    // Edge case for when the client somehow ends up outside the defined radius
                    if (session.LogicSettings.MaxTravelDistanceInMeters != 0 &&
                        distanceFromStart > session.LogicSettings.MaxTravelDistanceInMeters)
                    {
                        session.EventDispatcher.Send(new WarnEvent()
                        {
                            Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsOutsideRadius, distanceFromStart)
                        });
                        await Task.Delay(1000, cancellationToken);

                        await session.Navigation.HumanLikeWalking(
                            new GeoCoordinate(session.Settings.DefaultLatitude, session.Settings.DefaultLongitude),
                            session.LogicSettings.WalkingSpeedInKilometerPerHour, null, cancellationToken);
                    }

                    if (session.ForceMoveTo != null)
                    {
                        await ForceMoveTask.Execute(session, cancellationToken);
                        pokestopList = await GetPokeStops(session);
                    }


                    var displayStatsHit = 0;
                    var eggWalker = new EggWalker(1000, session);

                    if (pokestopList.Count <= 0)
                    {
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                        });
                    }

                    session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokestopList });

                    cancellationToken.ThrowIfCancellationRequested();

                    //resort
                    pokestopList =
                        pokestopList.OrderBy(
                            i =>
                                LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                    session.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
                    var pokeStop = pokestopList[0];
                    pokestopList.RemoveAt(0);

                    var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude, pokeStop.Latitude, pokeStop.Longitude);
                    var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                    session.EventDispatcher.Send(new FortTargetEvent { Id = fortInfo.FortId, Name = fortInfo.Name, Distance = distance, Latitude = fortInfo.Latitude, Longitude = fortInfo.Longitude, Description = fortInfo.Description, url = fortInfo.ImageUrls[0] });
                    if (session.LogicSettings.Teleport)
                        await session.Client.Player.UpdatePlayerLocation(fortInfo.Latitude, fortInfo.Longitude,
                            session.Client.Settings.DefaultAltitude);

                    else
                    {
                        await session.Navigation.HumanLikeWalking(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
                        session.LogicSettings.WalkingSpeedInKilometerPerHour,
                        async () =>
                        {
                        // Catch normal map Pokemon
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                        //Catch Incense Pokemon
                        await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                            return true;
                        }, cancellationToken);
                    }

                    FortSearchResponse fortSearch;
                    var timesZeroXPawarded = 0;
                    var fortTry = 0; //Current check
                    const int retryNumber = 50; //How many times it needs to check to clear softban
                    const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        fortSearch =
                            await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                        if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                        if (fortSearch.ExperienceAwarded == 0)
                        {
                            timesZeroXPawarded++;

                            if (timesZeroXPawarded > zeroCheck)
                            {
                                if ((int)fortSearch.CooldownCompleteTimestampMs != 0)
                                {
                                    break;
                                    // Check if successfully looted, if so program can continue as this was "false alarm".
                                }

                                fortTry += 1;

                                session.EventDispatcher.Send(new FortFailedEvent
                                {
                                    Name = fortInfo.Name,
                                    Try = fortTry,
                                    Max = retryNumber - zeroCheck
                                });
                                if (session.LogicSettings.Teleport)
                                    await Task.Delay(session.LogicSettings.DelaySoftbanRetry);
                                else
                                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 400);
                            }
                        }
                        else
                        {
                            session.EventDispatcher.Send(new FortUsedEvent
                            {
                                Id = pokeStop.Id,
                                Name = fortInfo.Name,
                                Exp = fortSearch.ExperienceAwarded,
                                Gems = fortSearch.GemsAwarded,
                                Items = StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded),
                                Latitude = pokeStop.Latitude,
                                Longitude = pokeStop.Longitude,
                                InventoryFull = fortSearch.Result == FortSearchResponse.Types.Result.InventoryFull,
                                Description = fortInfo.Description,
                                url = fortInfo.ImageUrls[0]
                            });

                            break; //Continue with program as loot was succesfull.
                        }
                    } while (fortTry < retryNumber - zeroCheck);
                    //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.


                    if (session.LogicSettings.Teleport)
                        await Task.Delay(session.LogicSettings.DelayPokestop);
                    else
                        await Task.Delay(1000, cancellationToken);


                    //Catch Lure Pokemon


                    if (pokeStop.LureInfo != null)
                    {
                        await CatchLurePokemonsTask.Execute(session, pokeStop, cancellationToken);
                    }
                    if (session.LogicSettings.Teleport)
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);

                    await eggWalker.ApplyDistance(distance, cancellationToken);

                    if (++stopsHit % 5 == 0) //TODO: OR item/pokemon bag is full
                    {
                        // need updated stardust information for upgrading, so refresh your profile now
                        await DownloadProfile(session);

                        if (fortSearch.ItemsAwarded.Count > 0)
                        {
                            await session.Inventory.RefreshCachedInventory();
                        }
                        await RecycleItemsTask.Execute(session, cancellationToken);
                        if (session.LogicSettings.EvolveAllPokemonWithEnoughCandy ||
                            session.LogicSettings.EvolveAllPokemonAboveIv)
                        {
                            await EvolvePokemonTask.Execute(session, cancellationToken);
                        }
                        if (session.LogicSettings.AutomaticallyLevelUpPokemon)
                        {
                            await LevelUpPokemonTask.Execute(session, cancellationToken);
                        }
                        if (session.LogicSettings.TransferDuplicatePokemon)
                        {
                            await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                        }
                        if (session.LogicSettings.RenamePokemon)
                        {
                            await RenamePokemonTask.Execute(session, cancellationToken);
                        }
                        if (++displayStatsHit >= 4)
                        {
                            await DisplayPokemonStatsTask.Execute(session);
                            displayStatsHit = 0;
                        }
                    }

                    if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                    {
                        await SnipePokemonTask.Execute(session, cancellationToken);
                    }
                }
            }
        }
        public static async Task NoTeleport(ISession session, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();

            var distanceFromStart = LocationUtils.CalculateDistanceInMeters(
                session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                session.Client.CurrentLatitude, session.Client.CurrentLongitude);

            // Edge case for when the client somehow ends up outside the defined radius
            if (session.LogicSettings.MaxTravelDistanceInMeters != 0 &&
                distanceFromStart > session.LogicSettings.MaxTravelDistanceInMeters)
            {
                session.EventDispatcher.Send(new WarnEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsOutsideRadius, distanceFromStart)
                });
                await Task.Delay(1000, cancellationToken);

                await session.Navigation.HumanLikeWalking(
                    new GeoCoordinate(session.Settings.DefaultLatitude, session.Settings.DefaultLongitude),
                    session.LogicSettings.WalkingSpeedInKilometerPerHour, null, cancellationToken);
            }

            var pokestopList = await GetPokeStops(session);
            var stopsHit = 0;
            var displayStatsHit = 0;
            var eggWalker = new EggWalker(1000, session);

            if (pokestopList.Count <= 0)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                });
            }

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokestopList });

            while (pokestopList.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (session.ForceMoveTo != null)
                {
                    await ForceMoveTask.Execute(session, cancellationToken);
                    pokestopList = await GetPokeStops(session);
                }

                //resort
                pokestopList =
                    pokestopList.OrderBy(
                        i =>
                            LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                session.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
                var pokeStop = pokestopList[0];
                pokestopList.RemoveAt(0);

                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                session.EventDispatcher.Send(new FortTargetEvent { Id = fortInfo.FortId, Name = fortInfo.Name, Distance = distance, Latitude = fortInfo.Latitude, Longitude = fortInfo.Longitude, Description = fortInfo.Description, url = fortInfo.ImageUrls[0] });
                if (session.LogicSettings.Teleport)
                    await session.Client.Player.UpdatePlayerLocation(fortInfo.Latitude, fortInfo.Longitude,
                        session.Client.Settings.DefaultAltitude);

                else
                {
                    await session.Navigation.HumanLikeWalking(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
                    session.LogicSettings.WalkingSpeedInKilometerPerHour,
                    async () =>
                    {
                        // Catch normal map Pokemon
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                        //Catch Incense Pokemon
                        await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                        return true;
                    }, cancellationToken);
                }

                FortSearchResponse fortSearch;
                var timesZeroXPawarded = 0;
                var fortTry = 0; //Current check
                const int retryNumber = 50; //How many times it needs to check to clear softban
                const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    fortSearch =
                        await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                    if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                    if (fortSearch.ExperienceAwarded == 0)
                    {
                        timesZeroXPawarded++;

                        if (timesZeroXPawarded > zeroCheck)
                        {
                            if ((int)fortSearch.CooldownCompleteTimestampMs != 0)
                            {
                                break;
                                // Check if successfully looted, if so program can continue as this was "false alarm".
                            }

                            fortTry += 1;

                            session.EventDispatcher.Send(new FortFailedEvent
                            {
                                Name = fortInfo.Name,
                                Try = fortTry,
                                Max = retryNumber - zeroCheck
                            });
                            if (session.LogicSettings.Teleport)
                                await Task.Delay(session.LogicSettings.DelaySoftbanRetry);
                            else
                                await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 400);
                        }
                    }
                    else
                    {
                        session.EventDispatcher.Send(new FortUsedEvent
                        {
                            Id = pokeStop.Id,
                            Name = fortInfo.Name,
                            Exp = fortSearch.ExperienceAwarded,
                            Gems = fortSearch.GemsAwarded,
                            Items = StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded),
                            Latitude = pokeStop.Latitude,
                            Longitude = pokeStop.Longitude,
                            InventoryFull = fortSearch.Result == FortSearchResponse.Types.Result.InventoryFull,
                            Description = fortInfo.Description,
                            url = fortInfo.ImageUrls[0]
                        });

                        break; //Continue with program as loot was succesfull.
                    }
                } while (fortTry < retryNumber - zeroCheck);
                //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.


                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayPokestop);
                else
                    await Task.Delay(1000, cancellationToken);


                //Catch Lure Pokemon


                if (pokeStop.LureInfo != null)
                {
                    await CatchLurePokemonsTask.Execute(session, pokeStop, cancellationToken);
                }
                if (session.LogicSettings.Teleport)
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);

                await eggWalker.ApplyDistance(distance, cancellationToken);

                if (++stopsHit % 5 == 0) //TODO: OR item/pokemon bag is full
                {
                    stopsHit = 0;
                    // need updated stardust information for upgrading, so refresh your profile now
                    await DownloadProfile(session);

                    if (fortSearch.ItemsAwarded.Count > 0)
                    {
                        await session.Inventory.RefreshCachedInventory();
                    }
                    await RecycleItemsTask.Execute(session, cancellationToken);
                    if (session.LogicSettings.EvolveAllPokemonWithEnoughCandy ||
                        session.LogicSettings.EvolveAllPokemonAboveIv)
                    {
                        await EvolvePokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.AutomaticallyLevelUpPokemon)
                    {
                        await LevelUpPokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.TransferDuplicatePokemon)
                    {
                        await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.RenamePokemon)
                    {
                        await RenamePokemonTask.Execute(session, cancellationToken);
                    }
                    if (++displayStatsHit >= 4)
                    {
                        await DisplayPokemonStatsTask.Execute(session);
                        displayStatsHit = 0;
                    }
                }

                if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                {
                    await SnipePokemonTask.Execute(session, cancellationToken);
                }
            }
        }

        private static async Task<List<FortData>> GetPokeStops(ISession session)
        {
            var mapObjects = await session.Client.Map.GetMapObjects();

            if (session.LogicSettings.Teleport)
            {
                var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
                return pokeStops.ToList();
            }
            else
            {
                var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
                return pokeStops.ToList();
            }
        }

        // static copy of download profile, to update stardust more accurately
        private static async Task DownloadProfile(ISession session)
        {
            session.Profile = await session.Client.Player.GetPlayer();
        }
    }
}