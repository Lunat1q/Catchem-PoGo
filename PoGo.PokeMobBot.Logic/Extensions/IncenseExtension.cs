using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Extensions
{
    public class CheckIncenseStatus
    {
        private static readonly DateTime Jan1st1970 = new DateTime
                (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        public static async Task<long> Execute(ISession session)
        {
            long timeRemainingIncense = new long();
            var currentMillis = CurrentTimeMillis();
            if ((session.Inventory.incenseExpiresMs == 0 || session.Inventory.incenseExpiresMs < currentMillis) 
                        && (currentMillis - session.Inventory.incenseLastUpdated) > 60000)
            {
                session.Inventory.incenseLastUpdated = currentMillis;
                List<AppliedItem> status = await session.Inventory.GetUsedItems();
                if (status.Count > 0)
                {
                    status.ForEach(delegate (AppliedItem singleAppliedItem)
                    {
                        if (singleAppliedItem.ItemType == ItemType.Incense)
                        {
                            var _expireMs = singleAppliedItem.ExpireMs;
                            var _appliedMs = singleAppliedItem.AppliedMs;
                            if (currentMillis < _expireMs)
                            {
                                timeRemainingIncense = _expireMs - currentMillis;
                                session.Inventory.incenseExpiresMs = _expireMs;
                            }
                            else
                            {
                                timeRemainingIncense = 0;
                                session.Inventory.incenseExpiresMs = 0;
                            }

                        }
                    });
                }
                else
                {
                    timeRemainingIncense = 0;
                }
            }
            else
            {
                timeRemainingIncense = session.Inventory.incenseExpiresMs - currentMillis;

            }
            return timeRemainingIncense;
        }
    }

    public class ResetAlreadyActiveIncense
    {
        public static async Task Execute(long resetAfter, ISession session)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(resetAfter));
        }
    }
}

