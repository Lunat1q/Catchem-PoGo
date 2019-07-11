using PoGo.PokeMobBot.Logic.State;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Logic;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class VerifyChallengeTask
    {
        public static async Task Execute(ISession session, string token)
        {
            var resp = await session.Client.Misc.VerifyChallenge(token);
            
            session.EventDispatcher.Send(new VerifyChallengeEvent
            {
              Success = resp.Success
            });
        }
    }
}
