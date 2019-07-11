using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonFavoriteEvent : IEvent
    {
        public ulong Uid;
        public PokemonId Pokemon;
        public int Cp;
        public double Iv;
        public int Candies;
        public bool Favoured;
    }
}