namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class InvalidKeepAmountEvent : IEvent
    {
        public int Count;
        public int Max;
    }
}