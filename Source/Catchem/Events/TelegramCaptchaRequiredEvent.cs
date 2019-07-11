using PoGo.PokeMobBot.Logic.Event;

namespace Catchem.Events
{
    public class TelegramCaptchaRequiredEvent : IEvent
    {
        public string BotNicName;
        public string ProfileName;
    }
}
