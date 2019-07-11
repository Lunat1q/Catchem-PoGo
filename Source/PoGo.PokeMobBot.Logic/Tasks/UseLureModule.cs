#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Fort;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Item;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using Logger = PoGo.PokeMobBot.Logic.Logging.Logger;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class UseLureModule
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken, string fortId)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lureCount = await session.Inventory.GetItemAmountByType(ItemId.ItemTroyDisk);
                if (lureCount <= 0)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "No Lure Modules"
                    });
                    return;
                }

                var pokestopList = await GetPokeStops(session);
                if (session.LogicSettings.UseEggIncubators && !session.EggWalker.Inited)
                {
                    await session.EggWalker.InitEggWalker(cancellationToken);
                }

                if (pokestopList.Count <= 0)
                {
                    return;
                }

                var targetPs = pokestopList.FirstOrDefault(x => x.Id == fortId);

                if (targetPs == null)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "Pokestop not found"
                    });
                    return;
                }

                if (targetPs.LureInfo != null)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = "Pokestop lured already!"
                    });
                    return;
                }

                var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, targetPs.Latitude, targetPs.Longitude);
                var fortInfo = await session.Client.Fort.GetFort(targetPs.Id, targetPs.Latitude, targetPs.Longitude);

                session.StartForceMove(targetPs.Latitude, targetPs.Longitude);
                var loops = 0;
                while (distance > 15 && loops++ < 500)
                {
                    distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude, targetPs.Latitude, targetPs.Longitude);
                    await Task.Delay(1500, cancellationToken);
                }
                if (distance > 15) return;


                var modifyReponse = await session.Client.Fort.AddFortModifier(fortInfo.FortId, ItemId.ItemTroyDisk);
                if (modifyReponse.Result == AddFortModifierResponse.Types.Result.SUCCESS)
                {
                    session.EventDispatcher.Send(new FortLureStartedEvent
                    {
                        Id = fortId,
                        Name = fortInfo.Name,
                        Dist = distance,
                        LureCountLeft = lureCount - 1
                    });
                    session.EventDispatcher.Send(new ItemLostEvent
                    {
                        Id = ItemId.ItemTroyDisk,
                        Count = 1
                    });
                }
                else
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message = $"Error while luring a pokestop, server response: {modifyReponse.Result}"
                    });
                }
            }
            catch (Exception ex)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "Using LureMod failed, see log file for the detail"
                });
                Logger.Write($"[UseLureMod] Failed with error: {ex.Message}");
            }
        }

        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {
            var pokeStops = await session.MapCache.FortDatas(session);

            pokeStops = pokeStops.Where(
                i =>
                    i.Used == false && i.Type == FortType.Checkpoint &&
                    ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                        LocationUtils.CalculateDistanceInMeters(
                            session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                            i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                    session.LogicSettings.MaxTravelDistanceInMeters == 0
                ).ToList();


            return pokeStops;
        }
    }
}