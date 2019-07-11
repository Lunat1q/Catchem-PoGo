namespace PoGo.PokeMobBot.Logic.Event.Logic
{
    public class BotCompleteFailureEvent : IEvent
    {
        public bool Shutdown;
        public bool Stop;
        public string Message;
    }
}
