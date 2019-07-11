#region using directives

using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event.Player;
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