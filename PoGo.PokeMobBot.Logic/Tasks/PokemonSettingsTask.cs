#region using directives

using System;
using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class PokemonSettingsTask
    {
        public static async Task Execute(ISession session, Action<IEvent> action)
        {
            var settings = await session.Inventory.GetPokemonSettings();

            action(new PokemonSettingsEvent
            {
                Data = settings.ToList()
            });

            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}