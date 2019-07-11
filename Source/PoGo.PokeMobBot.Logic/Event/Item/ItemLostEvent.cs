#region using directives

using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class ItemLostEvent : IEvent
    {
        public int Count;
        public ItemId Id;
    }
}