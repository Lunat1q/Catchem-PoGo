using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Logging;
using System.Threading.Tasks;
using POGOProtos.Networking.Responses;
using POGOProtos.Inventory.Item;
using Catchem.Classes;
using System.Threading;
using System.Collections.Generic;
using POGOProtos.Inventory;
using System;
using System.Diagnostics;

namespace Catchem.Extensions
{
    public class UseIncenseFromMenu
    {
        public static async Task<long> Execute(ISession session, ItemUiData item)
        {
            long currentIncenseStatus = await CheckIncenseStatus.Execute(session);
            if (currentIncenseStatus > 0 && currentIncenseStatus < 1800000)
            {
                Logger.Write("You are already using Incense");
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = "Incense already active"
                });
                Debug.WriteLine(currentIncenseStatus);
            }
            else
            {
                var itemToUse = new ItemId();
                switch (item.Name)
                {
                    case "ItemIncenseOrdinary":
                        itemToUse = ItemId.ItemIncenseOrdinary;
                        break;
                    case "ItemIncenseSpicy":
                        itemToUse = ItemId.ItemIncenseSpicy;
                        break;
                    case "ItemIncenseCool":
                        itemToUse = ItemId.ItemIncenseCool;
                        break;
                    case "ItemIncenseFloral":
                        itemToUse = ItemId.ItemIncenseFloral;
                        break;
                    default:
                        itemToUse = ItemId.ItemIncenseOrdinary;
                        break;
                }
                var currentAmountOfIncense = await session.Inventory.GetItemAmountByType(itemToUse);
                if (currentAmountOfIncense == 0)
                {
                    Logger.Write("No Incense available");
                    currentIncenseStatus = 0;
                }
                else
                {
                    var UseIncense = await session.Inventory.UseIncense(itemToUse);
                    if (UseIncense.Result == UseIncenseResponse.Types.Result.Success)
                    {
                        Logger.Write("Incense activated");
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = "Incense activated"
                        });
                        await Task.Delay(3000);
                        currentIncenseStatus = await CheckIncenseStatus.Execute(session);
                        Debug.WriteLine(currentIncenseStatus);
                    }
                    else if (UseIncense.Result == UseIncenseResponse.Types.Result.NoneInInventory)
                    {
                        Logger.Write("No Incense available");
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = "No Incense available"
                        });
                        currentIncenseStatus = 0;
                        Debug.WriteLine(currentIncenseStatus);
                    }
                    else if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive || (UseIncense.AppliedIncense == null))
                    {
                        Logger.Write("You are already using Incense");
                        session.EventDispatcher.Send(new WarnEvent
                        {
                            Message = "Incense already active"
                        });
                        Debug.WriteLine(currentIncenseStatus);
                    }
                    return currentIncenseStatus;
                }
            }
            return currentIncenseStatus;
        }
    }

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

    




