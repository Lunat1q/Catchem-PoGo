namespace PoGo.PokeMobBot.Logic.Event
{
    public class FortFailedEvent : IEvent
    {
        public int Max;
        public string Name;
        public int Try;
    }
}