#region using directives

using System;
using System.Collections.Generic;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class InventoryNewItemsEvent : IEvent
    {
        public List<Tuple<ItemId, int>> Items;
    }
}