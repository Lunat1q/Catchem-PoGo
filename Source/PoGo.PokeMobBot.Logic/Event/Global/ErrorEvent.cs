namespace PoGo.PokeMobBot.Logic.Event.Global
{
    public class ErrorEvent : IEvent
    {
        public string Message = "";

        public override string ToString()
        {
            return Message;
        }
    }
}