using PoGo.PokeMobBot.Logic.State;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Item;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Inventory.Item;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class UseItemTask
    {
        public static async Task Execute(ISession session, ItemId itemId, CancellationToken token)
        {
            var resp = await session.Inventory.UseItem(itemId, token);
            if (resp.Item1)
            {
                session.EventDispatcher.Send(new ItemLostEvent
                {
                    Id = itemId,
                    Count = 1
                });
                session.EventDispatcher.Send(new ItemUsedEvent
                {
                    Id = itemId,
                    ExpireMs = resp.Item2
                });
            }
            await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 10000);
        }
    }
}
