namespace PoGo.PokeMobBot.Logic.Event
{
    public class FortTargetEvent : IEvent
    {
        public string Id;
        public double Distance;
        public string Name;
        public string Description;
        public string url;
        public double Latitude;
        public double Longitude;
    }
}