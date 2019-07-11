#region using directives

using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event.Global;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class CheckBotStateTask
    {
        public static async Task<bool> Execute(ISession session, CancellationToken cancellationToken,
            bool silent = false)
        {
            if (session.State == BotState.Idle || session.State == BotState.Walk) return true;
            var attempt = 0;
            while (session.State != BotState.Idle && session.State != BotState.Walk && attempt < 61)
                //trying to get free status
            {
                attempt++;
                await Task.Delay(2000, cancellationToken);
            }
            if (attempt < 60 || session.State == BotState.Idle || session.State == BotState.Walk) return true;
            if (!silent)
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message =
                        "Character is too busy, probably you are trying to do 2 or more actions at the same time! Please, try later! If you didn't do anything - IGNORE that message, and don't send git issue!"
                });
            return false;
        }
    }
}