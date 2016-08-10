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
                await session.Client.Inventory.RecycleItem(item.ItemId, item.Count);
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item.ItemId, Count = item.Count });
                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayRecyleItem, cancellationToken);
                else
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 500);
            }

            await OptimizedRecycleBalls(session, cancellationToken);
            await OptimizedRecyclePotions(session, cancellationToken);
            await OptimizedRecycleRevives(session, cancellationToken);
            await OptimizedRecycleBerries(session, cancellationToken);

            await session.Inventory.RefreshCachedInventory();
        }

        private static async Task OptimizedRecycleBalls(ISession session, CancellationToken cancellationToken)
        {
            var pokeBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            var greatBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            var ultraBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            var masterBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMasterBall);
            int totalBallsCount = pokeBallsCount + greatBallsCount + ultraBallsCount + masterBallsCount;

            //var pokeBallsToKeep = session.LogicSettings.TotalAmountOfPokeballsToKeep;
            //var greatBallsToKeep = session.LogicSettings.TotalAmountOfGreatballsToKeep;
            //var ultraBallsToKeep = session.LogicSettings.TotalAmountOfUltraballsToKeep;
            //var masterBallsToKeep = session.LogicSettings.TotalAmountOfMasterballsToKeep;

            //int pokeBallsToRecycle = pokeBallsCount - pokeBallsToKeep;
            //int greatBallsToRecycle = greatBallsCount - greatBallsToKeep;
            //int ultraBallsToRecycle = ultraBallsCount - ultraBallsToKeep;
            //int masterBallsToRecycle = masterBallsCount - masterBallsToKeep;

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

            //int potionsToKeep = session.LogicSettings.TotalAmountOfPotionsToKeep;
            //int superPotionsToKeep = session.LogicSettings.TotalAmountOfSuperPotionsToKeep;
            //int hyperPotionsToKeep = session.LogicSettings.TotalAmountOfHyperPotionsToKeep;
            //int maxPotionsToKeep = session.LogicSettings.TotalAmountOfMaxPotionsToKeep;

            //int potionsToRecycle = potionCount - potionsToKeep;
            //int superPotionsToRecycle = superPotionCount - superPotionsToKeep;
            //int hyperPotionsToRecycle = hyperPotionsCount - hyperPotionsToKeep;
            //int maxPotionsToRecycle = maxPotionCount - maxPotionsToKeep;

            //if (!session.LogicSettings.AutomaticInventoryManagement)
            //{
            //    if (potionCount > potionsToKeep)
            //    {
            //        await RemoveItems(potionsToRecycle, ItemId.ItemPotion, cancellationToken, session);
            //    }
            //    if (superPotionCount > superPotionsToKeep)
            //    {
            //        await RemoveItems(superPotionsToRecycle, ItemId.ItemSuperPotion, cancellationToken, session);
            //    }
            //    if (hyperPotionsCount > hyperPotionsToKeep)
            //    {
            //        await RemoveItems(hyperPotionsToRecycle, ItemId.ItemHyperPotion, cancellationToken, session);
            //    }
            //    if (maxPotionCount > maxPotionsToKeep)
            //    {
            //        await RemoveItems(maxPotionsToRecycle, ItemId.ItemMaxPotion, cancellationToken, session);
            //    }
            //}
            //else
            //{
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

            int razzToKeep = session.LogicSettings.TotalAmountOfRazzToKeep;
            //int blukToKeep = session.LogicSettings.TotalAmountOfBlukToKeep;
            //int nanabToKeep = session.LogicSettings.TotalAmountOfNanabToKeep;
            //int pinapToKeep = session.LogicSettings.TotalAmountOfPinapToKeep;
            //int weparToKeep = session.LogicSettings.TotalAmountOfWeparToKeep;

            int razzToRecycle = razzCount - razzToKeep;
            //int blukToRecycle = blukCount - blukToKeep;
            //int nanabToRecycle = nanabCount - nanabToKeep;
            //int pinapToRecycle = pinapCount - pinapToKeep;
            //int weparToRecycle = weparCount - weparToKeep;

            if (!session.LogicSettings.AutomaticInventoryManagement)
            {
                if (razzCount > razzToKeep)
                {
                    await RemoveItems(razzToRecycle, ItemId.ItemRazzBerry, cancellationToken, session);
                }
                //if (blukCount > blukToKeep)
                //{
                //    await RemoveItems(blukToRecycle, ItemId.ItemBlukBerry, cancellationToken, session);
                //}
                //if nanabCount > nanabToKeep)
                //{
                //    await RemoveItems(nanabToRecycle, ItemId.ItemNanabBerry, cancellationToken, session);
                //}
                //if (pinapCount > pinapToKeep)
                //{
                //    await RemoveItems(pinapToRecycle, ItemId.ItemPinapBerry, cancellationToken, session);
                //}
                //if (weparCount > weparToKeep)
                //{
                //    await RemoveItems(weparToRecycle, ItemId.ItemWeparBerry, cancellationToken, session);
                //}
            }
            else
            {
                if (totalBerryCount > session.LogicSettings.AutomaticMaxAllBerries)
                {
                    _diff = totalBerryCount - session.LogicSettings.AutomaticMaxAllBerries;
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
            }
        }

        private static async Task OptimizedRecycleRevives(ISession session, CancellationToken cancellationToken)
        {
            var reviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemRevive);
            var maxReviveCount = await session.Inventory.GetItemAmountByType(ItemId.ItemMaxRevive);
            int totalRevivesCount = reviveCount + maxReviveCount;

            //var revivesToKeep = session.LogicSettings.TotalAmountOfRevivesToKeep;
            //var maxRevivesToKeep = session.LogicSettings.TotalAmountOfMaxRevivesToKeep;

            //int revivesToRecycle = reviveCount - revivesToKeep;
            //int maxRevivesToRecycle = maxReviveCount - maxRevivesToKeep;

            //if (!session.LogicSettings.AutomaticInventoryManagement)
            //{
            //    if (reviveCount > revivesToKeep)
            //    {
            //        await RemoveItems(revivesToRecycle, ItemId.ItemRevive, cancellationToken, session);
            //    }
            //    if (maxReviveCount > maxRevivesToKeep)
            //    {
            //        await RemoveItems(maxRevivesToRecycle, ItemId.ItemMaxRevive, cancellationToken, session);
            //    }
            //}
            //else
            //{
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
                // }
            }
        }

        private static async Task RemoveItems(int itemCount, ItemId item, CancellationToken cancellationToken, ISession session)
        {
            int itemsToRecycle;
            if (session.LogicSettings.AutomaticInventoryManagement)
            {  
                int itemsToKeep = itemCount - _diff;
                if (itemsToKeep < 0)
                {
                    itemsToKeep = 0;
                }
                itemsToRecycle = itemCount - itemsToKeep;
                _diff -= itemsToRecycle;
            } else
            {
                itemsToRecycle = itemCount;
            }
            if (itemsToRecycle != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await session.Client.Inventory.RecycleItem(item, itemsToRecycle);
                session.EventDispatcher.Send(new ItemRecycledEvent { Id = item, Count = itemsToRecycle });
                if (session.LogicSettings.Teleport)
                    await Task.Delay(session.LogicSettings.DelayRecyleItem, cancellationToken);
                else
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 500);
            }
        }
    }
}