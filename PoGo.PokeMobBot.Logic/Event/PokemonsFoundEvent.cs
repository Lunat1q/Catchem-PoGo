#region using directives

using System.Collections.Generic;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
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