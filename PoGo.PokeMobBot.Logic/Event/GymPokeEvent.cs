using POGOProtos.Data.Gym;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class GymPokeEvent : IEvent
    {
        public string Id;
        public double Distance;
        public string Name;
        public string Description;
        public GymState GymState;
    }
}