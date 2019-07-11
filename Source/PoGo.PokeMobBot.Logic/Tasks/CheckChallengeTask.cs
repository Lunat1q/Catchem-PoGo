using System;
using System.Threading;
using PoGo.PokeMobBot.Logic.State;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Logic;
using PokemonGo.RocketAPI.Extensions;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class CheckChallengeTask
    {
        public static async Task Execute(ISession session, CancellationToken token)
        {
            try
            {
                if (session.LastCaptchaRequest < DateTime.UtcNow.ToUnixTime())
                {
                    session.LastCaptchaRequest = DateTime.UtcNow.AddMinutes(5).ToUnixTime();
                    var resp = await session.Client.Misc.CheckChallenge();
                    if (resp.ShowChallenge)
                    {
                        session.EventDispatcher.Send(new CheckChallengeEvent
                        {
                            ChallengeUrl = resp.ChallengeUrl,
                            ShowChallenge = resp.ShowChallenge
                        });
                    }
                    await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, token);

                    await CheckChallengeDoneTask.Execute(session, token);
                }
            }
            catch
            {
                //ignore
            }
        }
    }
}
