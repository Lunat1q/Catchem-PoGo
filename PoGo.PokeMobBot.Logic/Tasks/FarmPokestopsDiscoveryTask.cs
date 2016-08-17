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


            while (pokestopList.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                               session.Client.CurrentLongitude, i.Latitude, i.Longitude)).First(x => !session.MapCache.CheckPokestopUsed(x));

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
                    await MoveToPokestop(session, cancellationToken, pokeStop);                

                if (!session.ForceMoveJustDone)
                {
                    var timesZeroXPawarded = 0;
                    var fortTry = 0; //Current check
                    const int retryNumber = 50; //How many times it needs to check to clear softban
                    const int zeroCheck = 5; //How many times it checks fort before it thinks it's softban
                    var shownSoftBanMessage = false;
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (session.MapCache.CheckPokestopUsed(pokeStop)) break; //already used somehow
                            var fortSearch = await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                        if (fortSearch.ExperienceAwarded > 0 && timesZeroXPawarded > 0) timesZeroXPawarded = 0;
                        if (fortSearch.ExperienceAwarded == 0)
                        {
                            if (TimesZeroXPawarded == 0) await MoveToPokestop(session, cancellationToken, pokeStop);
                            timesZeroXPawarded++;

                            if (timesZeroXPawarded <= zeroCheck) continue;
                            if ((int) fortSearch.CooldownCompleteTimestampMs != 0)
                            {
                                break;
                                // Check if successfully looted, if so program can continue as this was "false alarm".
                            }

                            fortTry += 1;

                            if (!shownSoftBanMessage || fortTry % 5 == 0)
                            {
                                session.EventDispatcher.Send(new FortFailedEvent
                                {
                                    Name = fortInfo.Name,
                                    Try = fortTry,
                                    Max = retryNumber - zeroCheck
                                });
                                shownSoftBanMessage = true;
                            }
                            if (session.LogicSettings.Teleport)
                                await Task.Delay(session.LogicSettings.DelaySoftbanRetry, cancellationToken);
                            else
                                await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 400);
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
                            session.MapCache.UsedPokestop(pokeStop);
                            RuntimeSettings.StopsHit++;
                            pokeStop.CooldownCompleteTimestampMS = DateTime.UtcNow.AddMinutes(5).ToUnixTime();
                            await CatchWildPokemonsTask.Execute(session, cancellationToken);
                            break; //Continue with program as loot was succesfull.
                        }
                    } while (fortTry < retryNumber - zeroCheck);

                    //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.
                    if (session.LogicSettings.Teleport)
                        await Task.Delay(session.LogicSettings.DelayPokestop, cancellationToken);
                    else
                        await Task.Delay(1000, cancellationToken);


                    //Catch Lure Pokemon


                    if (pokeStop.LureInfo != null)
                    {
                        await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                    }
                    if (session.LogicSettings.Teleport)
                        await CatchNearbyPokemonsTask.Execute(session, cancellationToken);

                    await eggWalker.ApplyDistance(distance, cancellationToken);
                }

                if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                {
                    await SnipePokemonTask.Execute(session, cancellationToken);
                }
            }
        }

        private static async Task MoveToPokestop(ISession session, CancellationToken cancellationToken, FortCacheItem pokeStop)
        {
            await session.Navigation.Move(new GeoCoordinate(pokeStop.Latitude, pokeStop.Longitude),
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
        }

        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

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
                            i.Type == FortType.Checkpoint &&
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
    }
}