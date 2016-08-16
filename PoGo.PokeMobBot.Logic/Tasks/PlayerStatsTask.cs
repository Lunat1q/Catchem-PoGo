#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory.Item;
using System;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class PlayerStatsTask
    {
        public static async Task Execute(ISession session, Action<IEvent> action)
        {
            var playersProfile = (await session.Inventory.GetPlayerStats())?
                .ToList();
            
            action(
                new PlayerStatsEvent
                {
                    PlayerStats = playersProfile,
                });

            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}