namespace PoGo.PokeMobBot.Logic.Event
{
    public class UpdatePositionEvent : IEvent
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
    }
}