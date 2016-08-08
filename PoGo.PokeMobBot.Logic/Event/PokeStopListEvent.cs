#region using directives

using System.Collections.Generic;
using POGOProtos.Map.Fort;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokeStopListEvent : IEvent
    {
        public IEnumerable<FortData> Forts;
    }
}