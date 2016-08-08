namespace PoGo.PokeMobBot.Logic.Event
{
    public class UseLuckyEggMinPokemonEvent : IEvent
    {
        public int Diff;
        public int CurrCount;
        public int MinPokemon;
    }
}