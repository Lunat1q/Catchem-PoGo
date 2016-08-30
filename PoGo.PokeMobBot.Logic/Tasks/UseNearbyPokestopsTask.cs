#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class UseNearbyPokestopsTask
    {
        //Please do not change GetPokeStops() in this file, it's specifically set
        //to only find stops within 40 meters
        //this is for gpx pathing, we are not going to the pokestops,
        //so do not make it more than 40 because it will never get close to those stops.
        public static async Task Execute(ISession session, CancellationToken cancellationToken, bool sendPokeStopsEvent = false)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pokestopList = await GetPokeStops(session);

            if (sendPokeStopsEvent)
                session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokestopList.Select(x => x.BaseFortData).ToList() });

            while (pokestopList.Any())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                pokestopList =
                    pokestopList.OrderBy(
                        i =>
                            LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                session.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
                var pokeStop = pokestopList[0];

                pokestopList.RemoveAt(0);
                
                if (pokeStop.Used)
                    continue;

                if (!session.LogicSettings.LootPokestops)
                {
                    session.MapCache.UsedPokestop(pokeStop, session);
                    continue;
                }
                var fortInfo = await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
                var fortSearch =
                    await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                await DelayingUtils.Delay(session.LogicSettings.DelayPokestop, 5000);
                if (fortSearch.ExperienceAwarded > 0)
                {
                    session.Runtime.StopsHit++;
                    session.Runtime.PokestopsToCheckGym--;
                    session.EventDispatcher.Send(new FortUsedEvent
                    {
                        Id = pokeStop.Id,
                        Name = fortInfo.Name,
                        Exp = fortSearch.ExperienceAwarded,
                        Gems = fortSearch.GemsAwarded,
                        Items = StringUtils.GetSummedFriendlyNameOfItemAwardList(fortSearch.ItemsAwarded),
                        Latitude = pokeStop.Latitude,
                        Longitude = pokeStop.Longitude
                    });
                    session.MapCache.UsedPokestop(pokeStop, session);
                    session.EventDispatcher.Send(new InventoryNewItemsEvent()
                    {
                        Items = fortSearch.ItemsAwarded.ToItemList()
                    });
                }
				if (pokeStop.LureInfo != null)
                {//because we're all fucking idiots for not catching this sooner
                    await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                }
            }
        }


        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);

            // Wasn't sure how to make this pretty. Edit as needed.
            pokeStops = pokeStops.Where(
                i =>
                    i.Used == false && i.Type == FortType.Checkpoint &&
                        i.CooldownCompleteTimestampMS < DateTime.UtcNow.ToUnixTime() &&
                        ( // Make sure PokeStop is within 40 meters or else it is pointless to hit it
                            LocationUtils.CalculateDistanceInMeters(
                                session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                i.Latitude, i.Longitude) < 40) ||
                        session.LogicSettings.MaxTravelDistanceInMeters == 0
                ).ToList();

            return pokeStops;

        }
    }
}