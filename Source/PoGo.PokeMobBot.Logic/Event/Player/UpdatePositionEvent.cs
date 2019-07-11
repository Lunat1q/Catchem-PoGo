namespace PoGo.PokeMobBot.Logic.Event.Player
{
    public class UpdatePositionEvent : IEvent
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
    }
}