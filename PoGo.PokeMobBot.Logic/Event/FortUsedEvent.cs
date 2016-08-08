namespace PoGo.PokeMobBot.Logic.Event
{
    public class FortUsedEvent : IEvent
    {
        public int Exp;
        public int Gems;
        public string Id;
        public bool InventoryFull;
        public string Items;
        public double Latitude;
        public double Longitude;
        public string Name;
        public string Description;
        public string url;
    }
}