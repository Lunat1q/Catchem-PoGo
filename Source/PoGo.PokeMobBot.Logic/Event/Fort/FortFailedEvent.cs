namespace PoGo.PokeMobBot.Logic.Event.Fort
{
    public class FortFailedEvent : IEvent
    {
        public int Max;
        public string Name;
        public int Try;
    }
}