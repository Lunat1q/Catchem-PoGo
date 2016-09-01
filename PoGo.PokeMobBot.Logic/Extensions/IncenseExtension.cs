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
            List<AppliedItem> status = await session.Inventory.GetUsedItems();
            long timeRemainingIncense = new long();
            if (status.Count > 0)
            {
                status.ForEach(delegate (AppliedItem singleAppliedItem)
                {
                    if (singleAppliedItem.ItemType == ItemType.Incense)
                    {
                        var _expireMs = singleAppliedItem.ExpireMs;
                        var _appliedMs = singleAppliedItem.AppliedMs;
                        var currentMillis = CurrentTimeMillis();
                        if (currentMillis < _expireMs)
                        {
                            timeRemainingIncense = _expireMs - currentMillis;
                        }
                        else
                        {
                            timeRemainingIncense = 0;
                        }

                    }
                });
            }
            else
            {
                timeRemainingIncense = 0;
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

