namespace PoGo.PokeMobBot.Logic.Event.Global
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