#region using directives

using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class RecycleSpecificItemTask
    {
        public static async Task Execute(ISession session, ItemId item, int amount, CancellationToken cancellationToken)
        {
            if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;
            var prevState = session.State;
            session.State = BotState.Recycle;
            cancellationToken.ThrowIfCancellationRequested();
            await session.Inventory.RefreshCachedInventory();
            var itemCount = await session.Inventory.GetItemAmountByType(item);
            if (itemCount < amount)
                amount = itemCount;
            await RemoveItems(amount, item, cancellationToken, session);
            await session.Inventory.RefreshCachedInventory();
            session.State = prevState;
        }
        private static async Task RemoveItems(int itemCount, ItemId item, CancellationToken cancellationToken, ISession session)
        {
            var itemsToRecycle = itemCount;
            if (itemsToRecycle != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item, Count = itemsToRecycle });
                await session.Client.Inventory.RecycleItem(item, itemsToRecycle);
                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayRecycleItem, cancellationToken);
                else
                    await DelayingUtils.Delay(session.LogicSettings.DelayRecycleItem, 500);
            }
        }
    }
}