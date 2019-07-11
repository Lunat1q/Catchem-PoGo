#region using directives

using System.Collections.Generic;
using POGOProtos.Data;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonListEvent : IEvent
    {
        public List<PokemonData> PokemonList;
    }
}