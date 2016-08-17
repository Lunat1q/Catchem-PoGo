using PoGo.PokeMobBot.Logic.State;
using System.Threading;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class MaintenanceTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var currentTotalItems = await session.Inventory.GetTotalItemCount();
            var recycleInventoryAtUsagePercentage = session.LogicSettings.RecycleInventoryAtUsagePercentage > 1
                ? session.LogicSettings.RecycleInventoryAtUsagePercentage / 100 : session.LogicSettings.RecycleInventoryAtUsagePercentage;

            if (RuntimeSettings.StopsHit %5 + session.Client.rnd.Next(5) == 0 || session.Profile.PlayerData.MaxItemStorage * recycleInventoryAtUsagePercentage < currentTotalItems)
            {
                RuntimeSettings.StopsHit = 0;
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
                //Do we need this?
                //await DisplayPokemonStatsTask.Execute(session);

            }
        }
        private static async Task DownloadProfile(ISession session)
        {
            session.Profile = await session.Client.Player.GetPlayer();
        }
    }


}
