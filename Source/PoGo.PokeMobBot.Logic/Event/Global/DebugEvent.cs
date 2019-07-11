namespace PoGo.PokeMobBot.Logic.Event.Global
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