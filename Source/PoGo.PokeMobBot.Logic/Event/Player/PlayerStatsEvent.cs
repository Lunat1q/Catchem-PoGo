#region using directives

using System.Collections.Generic;
using POGOProtos.Data.Player;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Player
{
    public class PlayerStatsEvent : IEvent

    {
        public List<PlayerStats> PlayerStats { get; set; }
    }
}
