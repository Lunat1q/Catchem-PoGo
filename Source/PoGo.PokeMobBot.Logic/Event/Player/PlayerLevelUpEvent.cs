namespace PoGo.PokeMobBot.Logic.Event.Player
{
    public class PlayerLevelUpEvent : IEvent
    {
        public int Level;
        public bool InventoryFull;
        public string Items;
    }
}