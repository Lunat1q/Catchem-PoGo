using PoGo.PokeMobBot.Logic.State;
using System.Threading;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class MaintenanceTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var prevState = session.State;
            session.State = BotState.Busy;
            var currentTotalItems = await session.Inventory.GetTotalItemCount();
            var recycleInventoryAtUsagePercentage = session.LogicSettings.RecycleInventoryAtUsagePercentage > 1
                ? session.LogicSettings.RecycleInventoryAtUsagePercentage / 100 : session.LogicSettings.RecycleInventoryAtUsagePercentage;

            if (session.Runtime.StopsHit + session.Client.rnd.Next(5) > 13 || session.Profile.PlayerData.MaxItemStorage * recycleInventoryAtUsagePercentage < currentTotalItems)
            {
                session.Runtime.StopsHit = 0;
                // need updated stardust information for upgrading, so refresh your profile now
                await DownloadProfile(session);

                await RecycleItemsTask.Execute(session, cancellationToken);
                if (session.LogicSettings.EvolveAllPokemonWithEnoughCandy ||
                    session.LogicSettings.EvolveAllPokemonAboveIv)
                {
                    await EvolvePokemonTask.Execute(session, cancellationToken);
                }
                if (session.LogicSettings.AutomaticallyLevelUpPokemon)
                {
                    await LevelUpPokemonTask.Execute(session, cancellationToken);
                }
                if (session.LogicSettings.TransferDuplicatePokemon)
                {
                    await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                }
                if (session.LogicSettings.RenamePokemon)
                {
                    await RenamePokemonTask.Execute(session, cancellationToken);
                }
            }
            session.State = prevState;
        }
        private static async Task DownloadProfile(ISession session)
        {
            session.Profile = await session.Client.Player.GetPlayer();
        }
    }


}
