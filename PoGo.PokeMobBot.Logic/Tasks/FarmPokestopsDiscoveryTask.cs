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
    public static class FarmPokeStopsDiscoveryTask
    {
        public static int TimesZeroXPawarded;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pokestopList = await GetPokeStops(session);
            var stopsHit = 0;
            var eggWalker = new EggWalker(1000, session);

            if (pokestopList.Count <= 0)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.FarmPokestopsNoUsableFound)
                });
            }

            session.EventDispatcher.Send(new PokeStopListEvent {Forts = pokestopList});


            while (pokestopList.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (session.ForceMoveTo != null)
                {
                    await ForceMoveTask.Execute(session, cancellationToken);
                }

                var newPokestopList = (await GetPokeStops(session)).OrderBy(i =>
                            LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                session.Client.CurrentLongitude, i.Latitude, i.Longitude)).Where(x => pokestopList.All(i => i.Id != x.Id) && LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                               session.Client.CurrentLongitude, x.Latitude, x.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters).ToList();
                session.EventDispatcher.Send(new PokeStopListEvent { Forts = newPokestopList });
                pokestopList.AddRange(newPokestopList);
                
                var pokeStop = pokestopList.OrderBy(i => LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                               session.Client.CurrentLongitude, i.Latitude, i.Longitude)).First(x => x.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());
                pokeStop.CooldownCompleteTimestampMs = DateTime.UtcNow.ToUnixTime() + 300 * 1000;

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
                    await moveToPokestop(session, cancellationToken, pokeStop);

                await CatchWildPokemonsTask.Execute(session, cancellationToken);

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
                        if (TimesZeroXPawarded == 0) await moveToPokestop(session, cancellationToken, pokeStop);
                        timesZeroXPawarded++;

                        if (timesZeroXPawarded > zeroCheck)
                        {
                            if ((int) fortSearch.CooldownCompleteTimestampMs != 0)
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
                            if(session.LogicSettings.Teleport)
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
                            InventoryFull = fortSearch.Result == FortSearchResponse.Types.Result.InventoryFull
                        });

                        break; //Continue with program as loot was succesfull.
                    }
                } while (fortTry < retryNumber - zeroCheck);
                    //Stop trying if softban is cleaned earlier or if 40 times fort looting failed.


                if(session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayPokestop);
                else
                    await Task.Delay(1000, cancellationToken);


                //Catch Lure Pokemon


                if (pokeStop.LureInfo != null)
                {
                    await CatchLurePokemonsTask.Execute(session, pokeStop, cancellationToken);
                }
                if(session.LogicSettings.Teleport)
                    await CatchNearbyPokemonsTask.Execute(session, cancellationToken);

                await eggWalker.ApplyDistance(distance, cancellationToken);

                if (stopsHit++ == 5 + session.Client.rnd.Next(5)) //TODO: OR item/pokemon bag is full
                {
                    stopsHit = 0;
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
                }

                if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                {
                    await SnipePokemonTask.Execute(session, cancellationToken);
                }
            }
        }

        private static async Task moveToPokestop(ISession session, CancellationToken cancellationToken, FortData pokeStop)
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

        private static async Task<List<FortData>> GetPokeStops(ISession session)
        {
            var mapObjects = await session.Client.Map.GetMapObjects();

            // Wasn't sure how to make this pretty. Edit as needed.
            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                .Where(
                    i =>
                        i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()                        
                );

            return pokeStops.ToList();
        }
    }
}