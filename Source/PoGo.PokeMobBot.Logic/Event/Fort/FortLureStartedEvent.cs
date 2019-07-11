namespace PoGo.PokeMobBot.Logic.Event.Fort
{
    public class FortLureStartedEvent : IEvent
    {
        public string Id;
        public string Name;
        public double Dist;
        public int LureCountLeft;
    }
}