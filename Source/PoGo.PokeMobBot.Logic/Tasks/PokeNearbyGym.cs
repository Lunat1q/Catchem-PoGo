#region using directives

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Fort;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Map.Fort;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class PokeNearbyGym
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (session.Runtime.PokestopsToCheckGym > 0) return;

            await CheckChallengeDoneTask.Execute(session, cancellationToken);
            await CheckChallengeTask.Execute(session, cancellationToken);

            var gymsNear = (await GetGyms(session)).OrderBy(i =>
                LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                    session.Client.CurrentLongitude, i.Latitude, i.Longitude))
                .ToList();
            if (gymsNear.Count > 0)
            {
                session.Runtime.PokestopsToCheckGym = 13 + session.Client.Rnd.Next(15);
                var nearestGym = gymsNear.FirstOrDefault();
                if (nearestGym != null)
                {
                    var gymInfo =
                        await
                            session.Client.Fort.GetGymDetails(nearestGym.Id, nearestGym.Latitude,
                                nearestGym.Longitude);
                    var gymDistance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                        session.Client.CurrentLongitude, nearestGym.Latitude, nearestGym.Longitude);
                    session.EventDispatcher.Send(new GymPokeEvent
                    {
                        Id = nearestGym.Id,
                        Name = gymInfo.Name,
                        Distance = gymDistance,
                        Description = gymInfo.Description,
                        GymState = gymInfo.GymState,
                        Lat = nearestGym.Latitude,
                        Lon = nearestGym.Longitude
                    });
                }
            }
        }

        private static async Task<List<FortCacheItem>> GetGyms(ISession session)
        {
            //var mapObjects = await session.Client.Map.GetMapObjects();

            List<FortCacheItem> gyms = await session.MapCache.GymDatas(session);

            //session.EventDispatcher.Send(new PokeStopListEvent { Forts = session.MapCache.baseFortDatas.ToList() });

            // Wasn't sure how to make this pretty. Edit as needed.
            gyms = gyms.Where(
                    i =>
                        i.Type == FortType.Gym &&
                        (LocationUtils.CalculateDistanceInMeters(
                                session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                        session.LogicSettings.MaxTravelDistanceInMeters == 0
                ).ToList();
            

            return gyms;
        }

    }
}