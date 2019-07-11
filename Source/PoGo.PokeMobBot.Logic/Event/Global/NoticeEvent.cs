namespace PoGo.PokeMobBot.Logic.Event.Global
{
    public class NoticeEvent : IEvent
    {
        public string Message = "";

        public override string ToString()
        {
            return Message;
        }
    }
}