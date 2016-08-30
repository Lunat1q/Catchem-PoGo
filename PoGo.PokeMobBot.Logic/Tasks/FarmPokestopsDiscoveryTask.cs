#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class FarmPokeStopsDiscoveryTask
    {
        public static int TimesZeroXPawarded;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pokestopList = await GetPokeStops(session);
            var eggWalker = new EggWalker(1000, session);

            if (pokestopList.Count <= 0)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                });
                if (session.ForceMoveJustDone)
                    session.ForceMoveJustDone = false;
                if (session.ForceMoveTo != null)
                {
                    await ForceMoveTask.Execute(session, cancellationToken);
                }
                await Task.Delay(60000, cancellationToken);
                await session.Navigation.Move(new GeoCoordinate(session.Client.CurrentLatitude + session.Client.rnd.NextInRange(-0.0001, 0.0001),
                                session.Client.CurrentLongitude + session.Client.rnd.NextInRange(-0.0001, 0.0001)),
                session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                async () =>
                {
                    // Catch normal map Pokemon
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                    //Catch Incense Pokemon
                    await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    return true;
                }, 
                async () =>
                {
                    await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                    return true;

                }, cancellationToken, session);
                pokestopList = await GetPokeStops(session);
            }

            session.EventDispatcher.Send(new PokeStopListEvent {Forts = pokestopList.Select(x=>x.BaseFortData)});

            var bestRoute = new List<GeoCoordinate>();

            session.Runtime.PokestopsToCheckGym = 13 + session.Client.rnd.Next(15); 

            while (pokestopList.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (session.Runtime.PokestopsToCheckGym <= 0)
                {
                    session.Runtime.PokestopsToCheckGym = 0;
                    var gymsNear = (await GetGyms(session)).OrderBy(i =>
                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                            session.Client.CurrentLongitude, i.Latitude, i.Longitude))
                        .ToList();
                    if (gymsNear.Count > 0)
                    {
                        session.Runtime.PokestopsToCheckGym = 13 + session.Client.rnd.Next(15);
                        var nearestGym = gymsNear.FirstOrDefault();
                        if (nearestGym != null)
                        {
                            var gymInfo = await session.Client.Fort.GetGymDetails(nearestGym.Id, nearestGym.Latitude, nearestGym.Longitude);
                            var gymDistance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                 session.Client.CurrentLongitude, nearestGym.Latitude, nearestGym.Longitude);
                            session.EventDispatcher.Send(new GymPokeEvent { Name = gymInfo.Name, Distance = gymDistance, Description = gymInfo.Description, GymState = gymInfo.GymState});
                        }
                    }
                }
                if (session.ForceMoveJustDone)
                    session.ForceMoveJustDone = false;

                if (session.ForceMoveTo != null)
                {
                    await ForceMoveTask.Execute(session, cancellationToken);
                }

                var newPokestopList = (await GetPokeStops(session)).OrderBy(i =>
                            LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                session.Client.CurrentLongitude, i.Latitude, i.Longitude)).Where(x => pokestopList.All(i => i.Id != x.Id) && LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                               session.Client.CurrentLongitude, x.Latitude, x.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters).ToList();
                session.EventDispatcher.Send(new PokeStopListEvent { Forts = newPokestopList.Select(x => x.BaseFortData) });
                pokestopList.AddRange(newPokestopList);
                
                var pokeStop = pokestopList.OrderBy(i => LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                               session.Client.CurrentLongitude, i.Latitude, i.Longitude)).FirstOrDefault(x => !session.MapCache.CheckPokestopUsed(x));

                if (pokeStop == null)
                {
                    await Task.Delay(60000, cancellationToken);
                    continue;
                }

                if (session.LogicSettings.RoutingService == RoutingService.GoogleDirections || session.LogicSettings.RoutingService == RoutingService.MapzenValhalla )
                {
//#if DEBUG
//                    bestRoute = RoutingUtils.GetBestRoute(pokeStop, pokestopList.Where(x => !session.MapCache.CheckPokestopUsed(x)), 10);
//#else
                    bestRoute = RoutingUtils.GetBestRoute(pokeStop, pokestopList.Where(x => !session.MapCache.CheckPokestopUsed(x)), 20);
//#endif
                    session.EventDispatcher.Send(new PokestopsOptimalPathEvent()
                    {
                        Coords = bestRoute.Select(x => Tuple.Create(x.Latitude, x.Longitude)).ToList()
                    });
                }


                var tooFarPokestops = pokestopList.Where(i => LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                               session.Client.CurrentLongitude, i.Latitude, i.Longitude) > session.LogicSettings.MaxTravelDistanceInMeters).ToList();

                foreach (var tooFar in tooFarPokestops)
                    pokestopList.Remove(tooFar);

                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                session.EventDispatcher.Send(new FortTargetEvent {Name = fortInfo.Name, Distance = distance});
                if(session.LogicSettings.Teleport)
                    await session.Client.Player.UpdatePlayerLocation(fortInfo.Latitude, fortInfo.Longitude,
                        session.Client.Settings.DefaultAltitude);
                else
                    await MoveToPokestop(session, cancellationToken, pokeStop, bestRoute, eggWalker);

                bestRoute.Clear();

                if (!session.LogicSettings.LootPokestops)
                {
                    session.MapCache.UsedPokestop(pokeStop, session);
                    continue;
                }

                if (!session.ForceMoveJustDone)
                {
                    var timesZeroXPawarded = 0;
                    var fortTry = 0; //Current check
                    const int retryNumber = 50; //How many times it needs to check to clear softban
                    const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                    //var shownSoftBanMessage = false;
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (session.MapCache.CheckPokestopUsed(pokeStop)) break; //already used somehow
                            var fortSearch = await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                        if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                        if (fortSearch.ExperienceAwarded == 0)
                        {
                            if (TimesZeroXPawarded == 0) await MoveToPokestop(session, cancellationToken, pokeStop, null, eggWalker);
                            timesZeroXPawarded++;
                            if ((int) fortSearch.CooldownCompleteTimestampMs != 0)
                            {
                                break;
                                // Check if successfully looted, if so program can continue as this was "false alarm".
                            }
                            if (timesZeroXPawarded <= zeroCheck) continue;

                            session.MapCache.UsedPokestop(pokeStop, session); //fuck that pokestop - skip it

                            break;
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
                                InventoryFull = fortSearch.Result == FortSearchResponse.Types.Result.InventoryFull
                            });
                            session.EventDispatcher.Send(new InventoryNewItemsEvent()
                            {
                                Items = fortSearch.ItemsAwarded.ToItemList()
                            });
                            session.MapCache.UsedPokestop(pokeStop, session);
                            session.Runtime.StopsHit++;
                            pokeStop.CooldownCompleteTimestampMS = DateTime.UtcNow.AddMinutes(5).ToUnixTime();
                            if (session.LogicSettings.CatchWildPokemon)
                            {
                                await CatchWildPokemonsTask.Execute(session, cancellationToken);
                            }
                            break; //Continue with program as loot was succesfull.
                        }
                    } while (fortTry < retryNumber - zeroCheck);

                    //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.
                    if (session.LogicSettings.Teleport)
                        await Task.Delay(session.LogicSettings.DelayPokestop, cancellationToken);
                    else
                        await Task.Delay(1000, cancellationToken);


                    //Catch Lure Pokemon


                    if (pokeStop.LureInfo != null && session.LogicSettings.CatchWildPokemon)
                    {
                        await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                    }
                    if (session.LogicSettings.Teleport && session.LogicSettings.CatchWildPokemon)
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);

                    await eggWalker.ApplyDistance(distance, cancellationToken);

                }
                session.Runtime.PokestopsToCheckGym--;
                if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                {
                    await SnipePokemonTask.Execute(session, cancellationToken);
                }
            }
        }

        private static async Task MoveToPokestop(ISession session, CancellationToken cancellationToken, FortCacheItem pokeStop, List<GeoCoordinate> waypoints, EggWalker eggWalker )
        {
            await session.Navigation.Move(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
                session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax,
                async () =>
                {
                    if (session.LogicSettings.CatchWildPokemon)
                    {
                        // Catch normal map Pokemon
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                        //Catch Incense Pokemon
                        await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                    }
                    return true;
                }, 
                async () =>
                {
                    await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                    return true;

                }, cancellationToken, session, waypointsToVisit: waypoints, eggWalker: eggWalker);
        }

        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);

            //session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                pokeStops = pokeStops.Where(
                    i =>
                        i.Used == false && i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                            LocationUtils.CalculateDistanceInMeters(
                                session.Client.CurrentLatitude, session.Client.CurrentLongitude,
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
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }

            return pokeStops;
        }

        private static async Task<List<FortCacheItem>> GetGyms(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> gyms = await session.MapCache.GymDatas(session);

            //session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                gyms = gyms.Where(
                    i =>
                        i.Type == FortType.Gym &&
                        i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                        (LocationUtils.CalculateDistanceInMeters(
                                session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                        session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }
            else
            {
                gyms = gyms.Where(
                        i =>
                            i.Type == FortType.Gym &&
                            (LocationUtils.CalculateDistanceInMeters(
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    ).ToList();
            }

            return gyms;
        }
    }
}