using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonUnFavoriteEvent : IEvent
    {
        public PokemonId Pokemon;
        public int Cp;
        public double Iv;
        public int Candies;
    }
}