using System.Threading;
using PoGo.PokeMobBot.Logic.State;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Logic;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class CheckChallengeDoneTask
    {
        public static async Task Execute(ISession session, CancellationToken token)
        {
            try
            {
                if (session.CaptchaChallenge)
                {
                    var prevState = session.State;
                    session.State = BotState.Captcha;
                    while (session.CaptchaChallenge)
                    {
                        await Task.Delay(5000, token);
                    }
                    session.State = prevState;
                    var resp = await session.Client.Misc.CheckChallenge();
                    session.EventDispatcher.Send(new CheckChallengeEvent
                    {
                        ChallengeUrl = resp.ChallengeUrl,
                        ShowChallenge = resp.ShowChallenge
                    });
                    await Task.Delay(2000, token);
                }
            }
            catch (TaskCanceledException)
            {
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = "Captcha not done!"
                });
            }
        }
    }
}
