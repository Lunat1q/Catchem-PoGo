namespace PoGo.PokeMobBot.Logic.Event.Fort
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