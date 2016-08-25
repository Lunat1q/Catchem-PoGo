#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Common;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class CheckBotStateTask
    {
        public static async Task<bool> Execute(ISession session, CancellationToken cancellationToken)
        {
            if (session.State == BotState.Idle || session.State == BotState.Walk) return true;
            var attempt = 0;
            while (session.State != BotState.Idle && session.State != BotState.Walk && attempt < 31) //trying to get free status
            {
                attempt++;
                await Task.Delay(2000, cancellationToken);
            }
            if (attempt < 30 || session.State == BotState.Idle || session.State == BotState.Walk) return true;
            session.EventDispatcher.Send(new WarnEvent()
            {
                Message = "Character is too busy, please, try later!"
            });
            return false;
        }
    }
}