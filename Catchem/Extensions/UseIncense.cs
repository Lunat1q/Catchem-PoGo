using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Logging;
using System.Threading.Tasks;
using POGOProtos.Networking.Responses;
using POGOProtos.Inventory.Item;
using Catchem.Classes;

public class UseIncenseFromMenu
{
    public static async Task<bool> Execute(ISession session, ItemUiData item)
    {
        var itemToUse = new ItemId();
        switch (item.Name)
        {
            case "ItemIncenseOrdinary":
                itemToUse = ItemId.ItemIncenseOrdinary;
                break;
            case "ItemIncenseSpicy":
                itemToUse = ItemId.ItemIncenseSpicy;
                break;
            case "ItemIncenseCool":
                itemToUse = ItemId.ItemIncenseCool;
                break;
            case "ItemIncenseFloral":
                itemToUse = ItemId.ItemIncenseFloral;
                break;
            default:
                itemToUse = ItemId.ItemIncenseOrdinary;
                break;
        }
        var currentAmountOfIncense = await session.Inventory.GetItemAmountByType(itemToUse);
        if (currentAmountOfIncense == 0)
        {
            Logger.Write("No Incense available");
            return false;
        }
        else
        {
            Logger.Write("Start using Incense");
        }
        var UseIncense = await session.Inventory.UseIncense(itemToUse);
        if (UseIncense.Result == UseIncenseResponse.Types.Result.Success)
        {
            Logger.Write("Incense activated");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "Incense activated"
            });
            return true;
        }
        else if (UseIncense.Result == UseIncenseResponse.Types.Result.NoneInInventory)
        {
            Logger.Write("No Incense available");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "No Incense available"
            });
            return false;
        }
        else if (UseIncense.Result == UseIncenseResponse.Types.Result.IncenseAlreadyActive || (UseIncense.AppliedIncense == null))
        {
            Logger.Write("You are already using Incense");
            session.EventDispatcher.Send(new WarnEvent
            {
                Message = "Incense already active"
            });
            return true;

        }
        return false;
    }
}

