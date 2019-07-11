#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event;
using System;
using PoGo.PokeMobBot.Logic.Event.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class InventoryListTask
    {
        public static async Task Execute(ISession session, Action<IEvent> action)
        {
            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory(true);

            var inventory = await session.Inventory.GetItems();

            action(
                new InventoryListEvent
                {
                    Items = inventory?.ToList()
                });

            var usedItems = await session.Inventory.GetUsedItems();
            foreach (var item in usedItems)
            {
                session.EventDispatcher.Send(new ItemUsedEvent()
                {
                    Id = item.ItemId,
                    ExpireMs = item.ExpireMs
                });
            }

            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}