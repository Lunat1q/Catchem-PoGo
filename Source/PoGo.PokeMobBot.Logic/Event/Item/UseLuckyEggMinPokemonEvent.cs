namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class UseLuckyEggMinPokemonEvent : IEvent
    {
        public int Diff;
        public int CurrCount;
        public int MinPokemon;
    }
}