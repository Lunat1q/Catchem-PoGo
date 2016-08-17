namespace PoGo.PokeMobBot.Logic.Event
{
    public class InvalidKeepAmountEvent : IEvent
    {
        public int Count;
        public int Max;
    }
}