namespace PoGo.PokeMobBot.Logic.Event
{
    public class DebugEvent : IEvent
    {
        public string Message = "";

        public override string ToString()
        {
            return Message;
        }
    }
}