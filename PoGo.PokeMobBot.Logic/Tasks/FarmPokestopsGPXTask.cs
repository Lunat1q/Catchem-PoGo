#region using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using GeoCoordinatePortable;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class FarmPokestopsGpxTask
    {
        private static DateTime _lastTasksCall = DateTime.Now;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var tracks = GetGpxTracks(session);
            var eggWalker = new EggWalker(1000, session);

            for (var curTrk = 0; curTrk < tracks.Count; curTrk++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var track = tracks.ElementAt(curTrk);
                var trackSegments = track.Segments;
                for (var curTrkSeg = 0; curTrkSeg < trackSegments.Count; curTrkSeg++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var trackPoints = track.Segments.ElementAt(0).TrackPoints;
                    for (var curTrkPt = 0; curTrkPt < trackPoints.Count; curTrkPt++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var nextPoint = trackPoints.ElementAt(curTrkPt);
                        var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                            session.Client.CurrentLongitude,
                            Convert.ToDouble(nextPoint.Lat, CultureInfo.InvariantCulture),
                            Convert.ToDouble(nextPoint.Lon, CultureInfo.InvariantCulture));

                        if (distance > 5000)
                        {
                            session.EventDispatcher.Send(new ErrorEvent
                            {
                                Message =
                                    session.Translation.GetTranslation(TranslationString.DesiredDestTooFar,
                                        nextPoint.Lat, nextPoint.Lon, session.Client.CurrentLatitude,
                                        session.Client.CurrentLongitude)
                            });
                            break;
                        }

                        var pokestopList = await GetPokeStops(session);
                        session.EventDispatcher.Send(new PokeStopListEvent {Forts = session.MapCache.baseFortDatas.ToList()});

                        while (pokestopList.Any())
                            // warning: this is never entered due to ps cooldowns from UseNearbyPokestopsTask 
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            pokestopList =
                                pokestopList.OrderBy(
                                    i =>
                                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                                            session.Client.CurrentLongitude, i.Latitude, i.Longitude)).ToList();
                            var pokeStop = pokestopList[0];
                            pokestopList.RemoveAt(0);

                            var fortInfo =
                                await session.Client.Fort.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                            if (pokeStop.LureInfo != null)
                            {
                                session.EventDispatcher.Send(new DebugEvent()
                                {
                                    Message = "This pokestop has a lure!"
                                });
                                await CatchLurePokemonsTask.Execute(session, pokeStop.BaseFortData, cancellationToken);
                            }

                            var fortSearch =
                                await session.Client.Fort.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                            if (fortSearch.ExperienceAwarded > 0)
                            {
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
                            }
                            if (fortSearch.ItemsAwarded.Count > 0)
                            {
                                await session.Inventory.RefreshCachedInventory();
                            }
                        }

                        if (DateTime.Now > _lastTasksCall)
                        {
                            _lastTasksCall =
                                DateTime.Now.AddMilliseconds(Math.Min(session.LogicSettings.DelayBetweenPlayerActions,
                                    3000));

                            await RecycleItemsTask.Execute(session, cancellationToken);

                            if (session.LogicSettings.SnipeAtPokestops || session.LogicSettings.UseSnipeLocationServer)
                            {
                                await SnipePokemonTask.Execute(session, cancellationToken);
                            }

                            if (session.LogicSettings.EvolveAllPokemonWithEnoughCandy ||
                                session.LogicSettings.EvolveAllPokemonAboveIv)
                            {
                                await EvolvePokemonTask.Execute(session, cancellationToken);
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

                        var targetLocation = new GeoCoordinate(Convert.ToDouble(trackPoints.ElementAt(curTrkPt).Lat, CultureInfo.InvariantCulture),
                Convert.ToDouble(trackPoints.ElementAt(curTrkPt).Lon, CultureInfo.InvariantCulture));

                        Navigation navi = new Navigation(session.Client);
						navi.UpdatePositionEvent += (lat, lng, alt) =>
                        {
                            session.EventDispatcher.Send(new UpdatePositionEvent { Latitude = lat, Longitude = lng, Altitude = alt});
                        };
                        var nextMoveSpeed = session.Client.rnd.NextInRange(session.LogicSettings.WalkingSpeedMin, session.LogicSettings.WalkingSpeedMax) * session.Settings.MoveSpeedFactor;

                        await navi.HumanPathWalking(
                            session,
                            targetLocation,
                            nextMoveSpeed,
                            async () =>
                            {
                                await CatchNearbyPokemonsTask.Execute(session, cancellationToken);
                                //Catch Incense Pokemon
                                await CatchIncensePokemonsTask.Execute(session, cancellationToken);
                                
                                return true;
                            },
                            async () => 
                            {
                                await UseNearbyPokestopsTask.Execute(session, cancellationToken);
                                return true;
                            },   
                            cancellationToken
                            );

                        await eggWalker.ApplyDistance(distance, cancellationToken);
                    } //end trkpts
                } //end trksegs
            } //end tracks
        }

        private static List<GpxReader.Trk> GetGpxTracks(ISession session)
        {
            var xmlString = File.ReadAllText(session.LogicSettings.GpxFile);
            var readgpx = new GpxReader(xmlString, session);
            return readgpx.Tracks;
        }

        //Please do not change GetPokeStops() in this file, it's specifically set
        //to only find stops within 40 meters
        //this is for gpx pathing, we are not going to the pokestops,
        //so do not make it more than 40 because it will never get close to those stops.
        private static async Task<List<FortCacheItem>> GetPokeStops(ISession session)
        {

            List<FortCacheItem> pokeStops = await session.MapCache.FortDatas(session);

            session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

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