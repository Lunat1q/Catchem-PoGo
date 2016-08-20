#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory.Item;
using System;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class SetPlayerTeamTask
    {
        public static async Task Execute(ISession session, TeamColor team)
        {
            if (session.Profile.PlayerData.Team != TeamColor.Neutral || team == TeamColor.Neutral) return;

            var teamResponse = await session.Client.Player.SetPlayerTeam(team);

            if (teamResponse.Status == SetPlayerTeamResponse.Types.Status.Success)
            {
                session.EventDispatcher.Send(new TeamSetEvent
                {
                    Color = team
                });
            }


            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}