#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class PlayerStatsTask
    {
        public static async Task Execute(ISession session)
        {
            var PlayersProfile = (await session.Inventory.GetPlayerStats())
                .ToList();


            session.EventDispatcher.Send(
                new PlayerStatsEvent
                {
                    PlayerStats = PlayersProfile,
                });
        }
    }
}