#region using directives

using System.Linq;
using System.Threading.Tasks;

using POGOProtos.Inventory.Item;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event;
using System;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class InventoryListTask
    {
        public static async Task Execute(ISession session, Action<IEvent> action)
        {
            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();

            var inventory = await session.Inventory.GetItems();

            action(
                new InventoryListEvent
                {
                    Items = inventory?.ToList()
                });

            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}