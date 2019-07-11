#region using directives

using System.Collections.Generic;
using POGOProtos.Map.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonsFoundEvent : IEvent
    {
        public IEnumerable<MapPokemon> Pokemons;
    }

    public class PokemonsWildFoundEvent : IEvent
    {
        public IEnumerable<WildPokemon> Pokemons;
    }
}