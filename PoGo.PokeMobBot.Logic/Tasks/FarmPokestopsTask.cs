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
using System.Security.Cryptography;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class FarmPokestopsTask
    {
        public static int TimesZeroXPawarded;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            
            Random random = new Random();
            if (session.LogicSettings.Teleport)
                await Teleport(session, cancellationToken, random);
            else
                await NoTeleport(session, cancellationToken, random);
        }

        public static async Task Teleport(ISession session, CancellationToken cancellationToken, Random random)
        {
            bool ShownSoftBanMessage = false;
            int stopsToHit = 20; //We should return to the main loop after some point, might as well limit this.
            //Not sure where else we could put this? Configs maybe if we incorporate
            //deciding how many pokestops in a row we want to hit before doing things like recycling?
            //might increase xp/hr not stopping every 5 stops. - Pocket


            //TODO: run through this with a fine-tooth comb and optimize it.
            var pokestopList = await GetPokeStops(session);

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokestopList.Select(x => x.BaseFortData).ToList() });

            for (int stopsHit = 0; stopsHit < stopsToHit; stopsHit++)
            {
                session.Runtime.BreakOutOfPathing = false;
                if (pokestopList.Count > 0)
                {
                    //start at 0 ends with 19 = 20 for the leechers{
                    cancellationToken.ThrowIfCancellationRequested();

                    var distanceFromStart = LocationUtils.CalculateDistanceInMeters(
                        session.Client.InitialLatitude, session.Client.InitialLongitude,
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
                        await session.Navigation.Move(
                            new GeoCoordinate(session.Settings.DefaultLatitude, session.Settings.DefaultLongitude),
                            session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax, null, null, cancellationToken, session);
                    }
                    if (session.ForceMoveJustDone)
                        session.ForceMoveJustDone = false;
                    if (session.ForceMoveTo != null)
                    {
                        await ForceMoveTask.Execute(session, cancellationToken);
                        pokestopList = await GetPokeStops(session);
                    }
                    var eggWalker = new EggWalker(1000, session);

                    if (pokestopList.Count <= 0)
                    {
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                        });
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    //resort
                    pokestopList =
                        pokestopList.OrderBy(
                            i => 
                                LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                    session.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();

                    if (session.LogicSettings.UsePokeStopLuckyNumber)
                    {
                        if (pokestopList.Count >= session.LogicSettings.PokestopSkipLuckyNumberMinUse)
                        {
                            int rng = random.Next(session.LogicSettings.PokestopSkipLuckyMin, session.LogicSettings.PokestopSkipLuckyMax);
#if DEBUG
                            Logger.Write("Skip Pokestop RNG: " + rng.ToString() + " against " + session.LogicSettings.PokestopSkipLuckyNumber.ToString(), LogLevel.Debug);
#endif
                            if (rng == session.LogicSettings.PokestopSkipLuckyNumber)
                            {
#if DEBUG
                                Logger.Write("Skipping Pokestop due to the rng god's will.", LogLevel.Debug);
#endif
                                pokestopList.RemoveAt(0);
                            }
                        }
                    }


                    var pokeStop = pokestopList[0];
                    pokestopList.RemoveAt(0);
                    session.Runtime.TargetStopID = pokeStop.Id;
                    var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude, pokeStop.Latitude, pokeStop.Longitude);
                    var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                    session.EventDispatcher.Send(new FortTargetEvent { Id = fortInfo.FortId, Name = fortInfo.Name, Distance = distance, Latitude = fortInfo.Latitude, Longitude = fortInfo.Longitude, Description = fortInfo.Description, url = fortInfo.ImageUrls[0] });
                    if (session.LogicSettings.Teleport)
                        await session.Client.Player.UpdatePlayerLocation(fortInfo.Latitude, fortInfo.Longitude,
                            session.Client.Settings.DefaultAltitude);

                    else
                    {
                        await session.Navigation.Move(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
                        session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                        async () =>
                        {
                            if (session.LogicSettings.CatchWildPokemon)
                            {
                                // Catch normal map Pokemon
                                await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                                //Catch Incense Pokemon remove this for time contraints
                                //await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                            }
                            return true;
                        }, 
                        async () =>
                        {
                            await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                            return true;
                        },
                        
                        cancellationToken, session);
                    }

                    if (!session.LogicSettings.LootPokestops)
                    {
                        session.MapCache.UsedPokestop(pokeStop, session);
                        continue;
                    }

                    if (!session.ForceMoveJustDone)
                    {
                        FortSearchResponse fortSearch;
                        var timesZeroXPawarded = 0;
                        var fortTry = 0; //Current check
                        const int retryNumber = 50; //How many times it needs to check to clear softban
                        const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                        if (session.Runtime.BreakOutOfPathing)
                            continue;
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
                                    if ((int) fortSearch.CooldownCompleteTimestampMs != 0)
                                    {
                                        break;
                                        // Check if successfully looted, if so program can continue as this was "false alarm".
                                    }

                                    fortTry += 1;

                                    if (!ShownSoftBanMessage)
                                    {
                                        session.EventDispatcher.Send(new FortFailedEvent
                                        {
                                            Name = fortInfo.Name,
                                            Try = fortTry,
                                            Max = retryNumber - zeroCheck
                                        });
                                        ShownSoftBanMessage = true;
                                    }
                                    await Task.Delay(session.LogicSettings.DelaySoftbanRetry);
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
                                session.MapCache.UsedPokestop(pokeStop, session);
                                session.EventDispatcher.Send(new InventoryNewItemsEvent()
                                {
                                    Items = fortSearch.ItemsAwarded.ToItemList()
                                });
                                break; //Continue with program as loot was succesfull.
                            }
                        } while (fortTry < retryNumber - zeroCheck);
                        //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.

                        ShownSoftBanMessage = false;
                        await Task.Delay(session.LogicSettings.DelayPokestop);


                        //Catch Lure Pokemon

                        if (session.LogicSettings.CatchWildPokemon)
                        {
                            if (pokeStop.LureInfo != null)
                            {
                                await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                            }
                            // Catch normal map Pokemon
                            if (session.LogicSettings.Teleport)
                                await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                            //Catch Incense Pokemon
                            await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                        }


                        await eggWalker.ApplyDistance(distance, cancellationToken);

                        await MaintenanceTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                    {
                        await SnipePokemonTask.Execute(session, cancellationToken);
                    }
                }
            }
        }

        public static async Task NoTeleport(ISession session, CancellationToken cancellationToken, Random random) { 
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

                await session.Navigation.Move(
                    new GeoCoordinate(session.Settings.DefaultLatitude, session.Settings.DefaultLongitude),
                    session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax, null, null, cancellationToken, session);
            }

            var pokestopList = await GetPokeStops(session);
            //var stopsHit = 0; //Replaced with RuntimeSettings.stopsHit;
            //var displayStatsHit = 0;

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokestopList.Select(x=>x.BaseFortData).ToList() });

            var eggWalker = new EggWalker(1000, session);

            if (pokestopList.Count <= 0)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                });
            }
            var bestRoute = new List<GeoCoordinate>();
            while (pokestopList.Any())
            {
                session.Runtime.BreakOutOfPathing = false;
                cancellationToken.ThrowIfCancellationRequested();
                if (session.ForceMoveJustDone)
                    session.ForceMoveJustDone = false;
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

                if (session.LogicSettings.UsePokeStopLuckyNumber)
                {
                    if (pokestopList.Count >= session.LogicSettings.PokestopSkipLuckyNumberMinUse)
                    {
                        int rng = random.Next(session.LogicSettings.PokestopSkipLuckyMin, session.LogicSettings.PokestopSkipLuckyMax);
#if DEBUG
                        Logger.Write("Skip Pokestop RNG: " + rng.ToString() + " against " + session.LogicSettings.PokestopSkipLuckyNumber.ToString(), LogLevel.Debug);
#endif
                        if (rng == session.LogicSettings.PokestopSkipLuckyNumber)
                        {
#if DEBUG
                            Logger.Write("Skipping Pokestop due to the rng god's will.", LogLevel.Debug);
#endif
                            if (pokestopList.Count > 0)
                                pokestopList.RemoveAt(0);
                        }
                    }
                }
                if (pokestopList.Count == 0)
                    break;

                var pokeStop = pokestopList[0];
                pokestopList.RemoveAt(0);

                if (session.LogicSettings.RoutingService == RoutingService.GoogleDirections || session.LogicSettings.RoutingService == RoutingService.MapzenValhalla)
                {
                    bestRoute = RoutingUtils.GetBestRoute(pokeStop, pokestopList.Where(x => !session.MapCache.CheckPokestopUsed(x)), 20);
                    session.EventDispatcher.Send(new PokestopsOptimalPathEvent()
                    {
                        Coords = bestRoute.Select(x => Tuple.Create(x.Latitude, x.Longitude)).ToList()
                    });
                }

                session.Runtime.TargetStopID = pokeStop.Id;
                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                session.EventDispatcher.Send(new FortTargetEvent { Id = fortInfo.FortId, Name = fortInfo.Name, Distance = distance,Latitude = fortInfo.Latitude, Longitude = fortInfo.Longitude, Description = fortInfo.Description, url = fortInfo.ImageUrls?.Count > 0 ? fortInfo.ImageUrls[0] : ""});

                    await session.Navigation.Move(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
                    session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                    async () =>
                    {
                        if (session.LogicSettings.CatchWildPokemon)
                        {
                            // Catch normal map Pokemon
                            await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                            
                            //Catch Incense Pokemon remove this for time constraints
                            //await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                        }
                        return true;
                    },
                    async () =>
                    {
                        await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                        return true;

                    } ,
                    cancellationToken, session, waypointsToVisit: bestRoute, eggWalker: eggWalker);
                if (!session.ForceMoveJustDone)
                {
                    var timesZeroXPawarded = 0;

                    var fortTry = 0; //Current check
                    const int retryNumber = 50; //How many times it needs to check to clear softban
                    const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                    if (session.Runtime.BreakOutOfPathing)
                        continue;
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (pokeStop.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime()) break; //already looted somehow
                        var fortSearch = await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                        if (fortSearch.Result == FortSearchResponse.Types.Result.InventoryFull)
                        {
                            await RecycleItemsTask.Execute(session, cancellationToken);
                        }
                        if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                        if (fortSearch.ExperienceAwarded == 0)
                        {
                            timesZeroXPawarded++;

                            if (timesZeroXPawarded > zeroCheck)
                            {
                                if ((int) fortSearch.CooldownCompleteTimestampMs != 0)
                                {
                                    break;
                                    // Check if successfully looted, if so program can continue as this was "false alarm".
                                }

                                fortTry += 1;


                                if (session.LogicSettings.Teleport)
                                {
                                    session.EventDispatcher.Send(new FortFailedEvent
                                    {
                                        Name = fortInfo.Name,
                                        Try = fortTry,
                                        Max = retryNumber - zeroCheck
                                    });
                                    await Task.Delay(session.LogicSettings.DelaySoftbanRetry, cancellationToken);
                                }
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
                                url = fortInfo.ImageUrls?[0]
                            });
                            session.Runtime.StopsHit++;
                            session.EventDispatcher.Send(new InventoryNewItemsEvent()
                            {
                                Items = fortSearch.ItemsAwarded.ToItemList()
                            });
                            session.MapCache.UsedPokestop(pokeStop, session);
                            break; //Continue with program as loot was succesfull.
                        }
                    } while (fortTry < 1);
                        //retryNumber - zeroCheck && fortSearch.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());
                    //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.


                    await Task.Delay(session.LogicSettings.DelayPokestop, cancellationToken);


                    //Catch Lure Pokemon

                    if (session.LogicSettings.CatchWildPokemon)
                    {
                        if (pokeStop.LureInfo != null)
                        {
                            await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                        }
                        if (session.LogicSettings.Teleport)
                            await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    }
                    await eggWalker.ApplyDistance(distance, cancellationToken);
                }
                if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                {
                    await SnipePokemonTask.Execute(session, cancellationToken);
                }
            }
        }

        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);
            

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                pokeStops = pokeStops.Where(
                    i =>
                        i.Used == false && i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                            LocationUtils.CalculateDistanceInMeters(
                                session.Client.InitialLatitude, session.Client.InitialLongitude,
                                i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                        session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }
            else
            {
                pokeStops = pokeStops.Where(
                        i =>
                            i.Used == false && i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Client.InitialLatitude, session.Client.InitialLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }

            return pokeStops;
        }

        // static copy of download profile, to update stardust more accurately
        private static async Task DownloadProfile(ISession session)
        {
            session.Profile = await session.Client.Player.GetPlayer();
        }
    }
}
