#region using directives

using System.Collections.Generic;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class InventoryListEvent : IEvent
    {
        public List<ItemData> Items;
    }
}