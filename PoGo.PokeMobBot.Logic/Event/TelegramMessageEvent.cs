namespace PoGo.PokeMobBot.Logic.Event
{
    public class TelegramMessageEvent : IEvent
    {
        public string Message;

        public override string ToString()
        {
            return Message;
        }
    }
}