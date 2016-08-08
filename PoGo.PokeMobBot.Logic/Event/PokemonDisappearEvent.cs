#region using directives

using System.Collections.Generic;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonDisappearEvent : IEvent
    {
        public MapPokemon Pokemon;
    }
}