#region using directives

using System.Linq;
using System.Threading.Tasks;

using POGOProtos.Inventory.Item;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class InventoryListTask
    {
        public static async Task Execute(ISession session)
        {
            var inventory = await session.Inventory.GetItems();

            session.EventDispatcher.Send(
                new InventoryListEvent
                {
                    Items = inventory.ToList()
                });
        }
    }
}