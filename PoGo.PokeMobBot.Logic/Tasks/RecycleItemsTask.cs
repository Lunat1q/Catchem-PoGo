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
    public class RecycleItemsTask
    {
        private static int diff;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await session.Inventory.RefreshCachedInventory();
            var currentTotalItems = await session.Inventory.GetTotalItemCount();
            if (session.Profile.PlayerData.MaxItemStorage * session.LogicSettings.RecycleInventoryAtUsagePercentage > currentTotalItems)
                return;
            var items = await session.Inventory.GetItemsToRecycle(session);
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await session.Client.Inventory.RecycleItem(item.ItemId, item.Count);
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item.ItemId, Count = item.Count });
                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayRecyleItem);
                else
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 500);
            }
            if (session.LogicSettings.TotalAmountOfPokeballsToKeep != 0)
            {
                await OptimizedRecycleBalls(session, cancellationToken);
            }

            if (session.LogicSettings.TotalAmountOfPotionsToKeep != 0)
            {
                await OptimizedRecyclePotions(session, cancellationToken);
            }

            if (session.LogicSettings.TotalAmountOfRevivesToKeep != 0)
            {
                await OptimizedRecycleRevives(session, cancellationToken);
            }
            await session.Inventory.RefreshCachedInventory();
        }

        private static async Task OptimizedRecycleBalls(ISession session, CancellationToken cancellationToken)
        {
            var pokeBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            var greatBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            var ultraBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            var masterBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMasterBall);

            int totalBallsCount = pokeBallsCount + greatBallsCount + ultraBallsCount + masterBallsCount;
            if (totalBallsCount > session.LogicSettings.TotalAmountOfPokeballsToKeep)
            {
                diff = totalBallsCount - session.LogicSettings.TotalAmountOfPokeballsToKeep;
                if (diff > 0)
                {
                    await RemoveItems(pokeBallsCount, ItemId.ItemPokeBall, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(greatBallsCount, ItemId.ItemGreatBall, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(ultraBallsCount, ItemId.ItemUltraBall, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(masterBallsCount, ItemId.ItemMasterBall, cancellationToken, session);
                }
            }
        }

        private static async Task OptimizedRecyclePotions(ISession session, CancellationToken cancellationToken)
        {
            var potionCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPotion);
            var superPotionCount = await session.Inventory.GetItemAmountByType(ItemId.ItemSuperPotion);
            var hyperPotionsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemHyperPotion);
            var maxPotionCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMaxPotion);

            int totalPotionsCount = potionCount + superPotionCount + hyperPotionsCount + maxPotionCount;
            if (totalPotionsCount > session.LogicSettings.TotalAmountOfPotionsToKeep)
            {
                diff = totalPotionsCount - session.LogicSettings.TotalAmountOfPotionsToKeep;
                if (diff > 0)
                {
                    await RemoveItems(potionCount, ItemId.ItemPotion, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(superPotionCount, ItemId.ItemSuperPotion, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(hyperPotionsCount, ItemId.ItemHyperPotion, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(maxPotionCount, ItemId.ItemMaxPotion, cancellationToken, session);
                }
            }
        }

        private static async Task OptimizedRecycleBerries(ISession session, CancellationToken cancellationToken)
        {
            var razz = await session.Inventory.GetItemAmountByType(ItemId.ItemRazzBerry);
            var bluk = await session.Inventory.GetItemAmountByType(ItemId.ItemBlukBerry);
            var nanab = await session.Inventory.GetItemAmountByType(ItemId.ItemNanabBerry);
            var pinap = await session.Inventory.GetItemAmountByType(ItemId.ItemPinapBerry);
            var wepar = await session.Inventory.GetItemAmountByType(ItemId.ItemWeparBerry);

            int totalBerryCount = razz + bluk + nanab + pinap + wepar;
            if (totalBerryCount > session.LogicSettings.TotalAmountOfBerriesToKeep)
            {
                diff = totalBerryCount - session.LogicSettings.TotalAmountOfPotionsToKeep;
                if (diff > 0)
                {
                    await RemoveItems(razz, ItemId.ItemRazzBerry, cancellationToken, session);
                }

                if (diff > 0)
                {
                    await RemoveItems(bluk, ItemId.ItemBlukBerry, cancellationToken, session);
                }

                if (diff > 0)
                {
                    await RemoveItems(nanab, ItemId.ItemNanabBerry, cancellationToken, session);
                }

                if (diff > 0)
                {
                    await RemoveItems(pinap, ItemId.ItemPinapBerry, cancellationToken, session);
                }

                if (diff > 0)
                {
                    await RemoveItems(wepar, ItemId.ItemWeparBerry, cancellationToken, session);
                }
            }
        }

        private static async Task OptimizedRecycleRevives(ISession session, CancellationToken cancellationToken)
        {
            var reviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemRevive);
            var maxReviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMaxRevive);

            int totalRevivesCount = reviveCount + maxReviveCount;
            if (totalRevivesCount > session.LogicSettings.TotalAmountOfRevivesToKeep)
            {
                diff = totalRevivesCount - session.LogicSettings.TotalAmountOfRevivesToKeep;
                if (diff > 0)
                {
                    await RemoveItems(reviveCount, ItemId.ItemRevive, cancellationToken, session);
                }
                if (diff > 0)
                {
                    await RemoveItems(maxReviveCount, ItemId.ItemMaxRevive, cancellationToken, session);
                }
            }
        }

        private static async Task RemoveItems(int itemCount, ItemId item, CancellationToken cancellationToken, ISession session)
        {
            int itemsToRecycle = 0;
            int itemsToKeep = itemCount - diff;
            if (itemsToKeep < 0)
            {
                itemsToKeep = 0;
            }
            itemsToRecycle = itemCount - itemsToKeep;

            if (itemsToRecycle != 0)
            {
                diff -= itemsToRecycle;
                cancellationToken.ThrowIfCancellationRequested();
                await session.Client.Inventory.RecycleItem(item, itemsToRecycle);
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item, Count = itemsToRecycle });
                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayRecyleItem);
                else
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 500);
            }
        }
    }
}