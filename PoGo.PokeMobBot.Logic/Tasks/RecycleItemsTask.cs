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
        private static int _diff;

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var prevState = session.State;
            session.State = BotState.Recycle;
            cancellationToken.ThrowIfCancellationRequested();
            await session.Inventory.RefreshCachedInventory();
            var currentTotalItems = await session.Inventory.GetTotalItemCount();
            var recycleInventoryAtUsagePercentage = session.LogicSettings.RecycleInventoryAtUsagePercentage > 1
                ? session.LogicSettings.RecycleInventoryAtUsagePercentage / 100 : session.LogicSettings.RecycleInventoryAtUsagePercentage;
            if (session.Profile.PlayerData.MaxItemStorage * recycleInventoryAtUsagePercentage > currentTotalItems)
                return;
            var items = await session.Inventory.GetItemsToRecycle(session);
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item.ItemId, Count = item.Count });
                await session.Client.Inventory.RecycleItem(item.ItemId, item.Count);
                await Task.Delay(session.LogicSettings.DelayRecycleItem, cancellationToken);
            }

            await OptimizedRecycleBalls(session, cancellationToken);
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            await OptimizedRecyclePotions(session, cancellationToken);
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            await OptimizedRecycleRevives(session, cancellationToken);
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            await OptimizedRecycleBerries(session, cancellationToken);
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            await session.Inventory.RefreshCachedInventory();
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);
            session.State = prevState;
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
                _diff = totalBallsCount - session.LogicSettings.TotalAmountOfPokeballsToKeep;
                if (_diff > 0)
                {
                    await RemoveItems(pokeBallsCount, ItemId.ItemPokeBall, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(greatBallsCount, ItemId.ItemGreatBall, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(ultraBallsCount, ItemId.ItemUltraBall, cancellationToken, session);
                }
                if (_diff > 0)
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
                _diff = totalPotionsCount - session.LogicSettings.TotalAmountOfPotionsToKeep;
                if (_diff > 0)
                {
                    await RemoveItems(potionCount, ItemId.ItemPotion, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(superPotionCount, ItemId.ItemSuperPotion, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(hyperPotionsCount, ItemId.ItemHyperPotion, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(maxPotionCount, ItemId.ItemMaxPotion, cancellationToken, session);
                }
                // }
            }
        }

        private static async Task OptimizedRecycleBerries(ISession session, CancellationToken cancellationToken)
        {
            var razzCount = await session.Inventory.GetItemAmountByType(ItemId.ItemRazzBerry);
            var blukCount = await session.Inventory.GetItemAmountByType(ItemId.ItemBlukBerry);
            var nanabCount = await session.Inventory.GetItemAmountByType(ItemId.ItemNanabBerry);
            var pinapCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPinapBerry);
            var weparCount = await session.Inventory.GetItemAmountByType(ItemId.ItemWeparBerry);
            int totalBerryCount = razzCount + blukCount + nanabCount + pinapCount + weparCount;


            _diff = totalBerryCount - session.LogicSettings.TotalAmountOfRazzToKeep;
            if (_diff > 0)
            {
                await RemoveItems(razzCount, ItemId.ItemRazzBerry, cancellationToken, session);
            }
            if (_diff > 0)
            {
                await RemoveItems(blukCount, ItemId.ItemBlukBerry, cancellationToken, session);
            }
            if (_diff > 0)
            {
                await RemoveItems(nanabCount, ItemId.ItemNanabBerry, cancellationToken, session);
            }
            if (_diff > 0)
            {
                await RemoveItems(pinapCount, ItemId.ItemPinapBerry, cancellationToken, session);
            }
            if (_diff > 0)
            {
                await RemoveItems(weparCount, ItemId.ItemWeparBerry, cancellationToken, session);
            }
        }

        private static async Task OptimizedRecycleRevives(ISession session, CancellationToken cancellationToken)
        {
            var reviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemRevive);
            var maxReviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMaxRevive);
            int totalRevivesCount = reviveCount + maxReviveCount;
           
            if (totalRevivesCount > session.LogicSettings.TotalAmountOfRevivesToKeep)
            {
                _diff = totalRevivesCount - session.LogicSettings.TotalAmountOfRevivesToKeep;
                if (_diff > 0)
                {
                    await RemoveItems(reviveCount, ItemId.ItemRevive, cancellationToken, session);
                }
                if (_diff > 0)
                {
                    await RemoveItems(maxReviveCount, ItemId.ItemMaxRevive, cancellationToken, session);
                }
            }
        }

        private static async Task RemoveItems(int itemCount, ItemId item, CancellationToken cancellationToken, ISession session)
        {
            var itemsToKeep = itemCount - _diff;
            if (itemsToKeep < 0)
            {
                itemsToKeep = 0;
            }
            var itemsToRecycle = itemCount - itemsToKeep;
            _diff -= itemsToRecycle;

            if (itemsToRecycle != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item, Count = itemsToRecycle });
                await session.Client.Inventory.RecycleItem(item, itemsToRecycle);
                await Task.Delay(session.LogicSettings.DelayRecycleItem, cancellationToken);
            }
        }
    }
}