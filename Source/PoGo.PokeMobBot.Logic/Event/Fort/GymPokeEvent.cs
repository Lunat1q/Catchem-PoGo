using POGOProtos.Data.Gym;

namespace PoGo.PokeMobBot.Logic.Event.Fort
{
    public class GymPokeEvent : IEvent
    {
        public string Id;
        public double Distance;
        public string Name;
        public string Description;
        public GymState GymState;
        public double Lat;
        public double Lon;
    }
}