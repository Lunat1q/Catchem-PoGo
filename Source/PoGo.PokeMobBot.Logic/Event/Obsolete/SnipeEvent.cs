namespace PoGo.PokeMobBot.Logic.Event.Obsolete
{
    public class SnipeEvent : IEvent
    {
        public string Message = "";

        public override string ToString()
        {
            return Message;
        }
    }
}