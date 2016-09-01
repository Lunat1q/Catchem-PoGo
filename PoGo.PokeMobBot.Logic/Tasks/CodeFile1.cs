using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Inventory.Item;
using System;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;
using System.Diagnostics;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class CheckIncenseActiveTask
    {
        public static async Task Execute(ISession session)
        {
            Debug.WriteLine("test");
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}