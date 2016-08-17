#region using directives

using System.Linq;
using System.Threading.Tasks;

using POGOProtos.Inventory.Item;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event;
using POGOProtos.Data.Player;
using System.Collections.Generic;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PlayerStatsEvent : IEvent

    {
        public List<PlayerStats> PlayerStats { get; set; }
    }
}
