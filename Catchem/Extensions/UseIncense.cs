using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Logging;
using System.Threading.Tasks;
using POGOProtos.Networking.Responses;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;

public class UseIncenseFromMenu
{
    public static async Task Execute(ISession session)
    {
        await session.Inventory.RefreshCachedInventory();
        var currentAmountOfIncense = await session.Inventory.GetItemAmountByType(ItemId.ItemIncenseOrdinary);
        if (currentAmountOfIncense == 0)
        {
            Logger.Write("No Incense available");
            return;
        }
        else
        {
            Logger.Write("Start using Incense");

        }
        var UseIncense = await session.Inventory.UseIncense();
        if (UseIncense.Result == UseIncenseResponse.Types.Result.Success)
        {
            Logger.Write("Incense activated");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "Incense activated"
            });
        }
        else if (UseIncense.Result == UseIncenseResponse.Types.Result.NoneInInventory)
        {
            Logger.Write("Huh?");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "No Incense available"
            });
        }
        else if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive || (UseIncense.AppliedIncense == null))
        {
            Logger.Write("You are already using Incense");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "Incense already active"
            });
        }
    }
}
namespace PokemonGo.RocketAPI.Rpc
{
    public async Task<bool> CheckIncense()
    {
        await ;

    }
}
