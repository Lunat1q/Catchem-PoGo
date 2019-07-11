using POGOProtos.Inventory.Item;

namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class ItemUsedEvent : IEvent
    {
        public ItemId Id;
        public long ExpireMs;
    }
}