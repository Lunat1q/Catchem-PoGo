using PoGo.PokeMobBot.Logic.Event;

namespace Catchem.Events
{
    public class TelegramPlayerLevelUpEvent : IEvent
    {
        public int Level;
        public bool InventoryFull;
        public string Items;
        public string BotNicName;
        public string ProfileName;
    }
}
