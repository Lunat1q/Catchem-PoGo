using System;
using System.Linq;
using PoGo.PokeMobBot.Logic.State;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Logging;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class ActionQueueTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            if (session.ActionQueue != null && session.ActionQueue.Count > 0)
            {
               
                while (session.ActionQueue.Count > 0)
                {
                    try
                    {
                        var action = session.ActionQueue.FirstOrDefault();
                        if (action == null) continue;
                        session.ActionQueue.Remove(action);
                        await action.Action();
                        await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        session.ActionQueue.Clear();
                    }
                    catch (Exception ex)
                    {
                        Logger.Write("[ACTION QUEUE FAILURE] " + ex.Message);
                    }
                }
            }
        }
    }

    public class ManualAction
    {
        public string Uid;
        public ISession Session;
        public string Name { get; set; }
        public ulong BindedPokeUid;

        public Func<Task<bool>> Action;
    }


}
