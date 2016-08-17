using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonFavoriteEvent : IEvent
    {
        public PokemonId Pokemon;
        public int Cp;
        public double Iv;
        public int Candies;
    }
}