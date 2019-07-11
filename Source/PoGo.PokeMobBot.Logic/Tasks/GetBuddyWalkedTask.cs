using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.State;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    internal class GetBuddyWalkedTask
    {
        public static async Task Execute(ISession session, CancellationToken token)
        {
            var resp = await session.Client.Inventory.GetBuddyWalked();

            //if (resp.FamilyCandyId != PokemonFamilyId.FamilyUnset)
            //{
                session.EventDispatcher.Send(new BuddyWalkedEvent
                {
                    CandyEarnedCount = resp.CandyEarnedCount,
                    FamilyCandyId = resp.FamilyCandyId,
                    Success = resp.Success
                });
            //}
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, token);
        }
    }
}
